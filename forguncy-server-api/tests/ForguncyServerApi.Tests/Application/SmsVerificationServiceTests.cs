using ForguncyServerApi.Application;
using Xunit;

namespace ForguncyServerApi.Tests.Application;

public sealed class SmsVerificationServiceTests
{
    private static readonly EnterpriseIdentity Identity =
        new(7, "91330200SYNTHETIC");

    private const string Mobile = "13800000000";

    private static readonly DateTimeOffset Now =
        new(2026, 8, 7, 12, 34, 48, TimeSpan.FromHours(8));

    [Fact]
    public async Task SendAsync_authenticates_updates_token_saves_code_logs_and_sends_sms()
    {
        var fixture = CreateFixture(code: "123456");

        var result = await fixture.Service.SendAsync(Identity, Mobile, CancellationToken.None);

        Assert.Equal(SendVerificationCodeStatus.Success, result.Status);
        Assert.Equal(Now.AddMinutes(5), result.ExpiresAt);
        Assert.Equal(Now.AddSeconds(60), result.RetryAt);
        Assert.Equal("mock-auth-token", fixture.Config.Values["token"]);
        Assert.Equal(
            new SmsAuthenticationRequest("client-id", "client-secret", "tenant-a"),
            fixture.Authentication.Request);

        var saved = Assert.IsType<VerificationCodeRecord>(fixture.Codes.Record);
        Assert.Equal(Identity.CreditCode, saved.CreditCode);
        Assert.Equal(Mobile, saved.Mobile);
        Assert.Equal("123456", saved.Code);
        Assert.Equal(Now.AddMinutes(5), saved.ExpiresAt);
        Assert.Equal(Now.AddSeconds(60), saved.RetryAt);
        Assert.Null(saved.VerifiedAt);

        var log = Assert.Single(fixture.MessageLogs.Added);
        Assert.Equal("system", log.Sender);
        Assert.Equal(Mobile, log.Mobile);
        Assert.Equal("您好，您的用地需求填报验证码是：【123456】", log.Content);
        Assert.Equal("企业", log.Reciveder);
        Assert.Equal("msg-2026080748-000000001", log.TransactionId);

        Assert.Equal(
            new SmsSendRequest(
                Mobile,
                log.Content,
                log.TransactionId,
                "sms-code",
                "sms-secret",
                "mock-auth-token"),
            fixture.Gateway.Request);
        var update = Assert.Single(fixture.MessageLogs.Updates);
        Assert.Equal(log.TransactionId, update.TransactionId);
        Assert.Equal(Now, update.Date);
        Assert.Equal("调用成功!", update.State);
    }

    [Fact]
    public async Task SendAsync_reuses_an_unexpired_code_for_the_same_enterprise_and_mobile()
    {
        var fixture = CreateFixture(
            existing: new VerificationCodeRecord(
                19,
                Identity.CreditCode,
                Mobile,
                "654321",
                Now.AddMinutes(3),
                Now.AddSeconds(-1),
                null),
            code: "000000");

        var result = await fixture.Service.SendAsync(Identity, Mobile, CancellationToken.None);

        Assert.Equal(SendVerificationCodeStatus.Success, result.Status);
        Assert.Equal(0, fixture.CodeGenerator.CallCount);
        Assert.Equal(19, fixture.Codes.Record?.Id);
        Assert.Equal("654321", fixture.Codes.Record?.Code);
        Assert.Equal(Now.AddMinutes(3), result.ExpiresAt);
        Assert.Equal(Now.AddSeconds(60), result.RetryAt);
        Assert.Contains("【654321】", Assert.Single(fixture.MessageLogs.Added).Content);
    }

    [Fact]
    public async Task SendAsync_returns_cooldown_without_creating_a_second_message()
    {
        var fixture = CreateFixture(
            existing: new VerificationCodeRecord(
                19,
                Identity.CreditCode,
                Mobile,
                "654321",
                Now.AddMinutes(3),
                Now.AddSeconds(30),
                null));

        var result = await fixture.Service.SendAsync(Identity, Mobile, CancellationToken.None);

        Assert.Equal(SendVerificationCodeStatus.Cooldown, result.Status);
        Assert.Equal(Now.AddMinutes(3), result.ExpiresAt);
        Assert.Equal(Now.AddSeconds(30), result.RetryAt);
        Assert.Equal(0, fixture.CodeGenerator.CallCount);
        Assert.Null(fixture.Gateway.Request);
        Assert.Empty(fixture.MessageLogs.Added);
        Assert.Empty(fixture.MessageLogs.Updates);
    }

    [Fact]
    public async Task SendAsync_updates_the_log_with_the_gateway_message_when_send_fails()
    {
        var fixture = CreateFixture(
            gatewayResult: new SmsSendResult(false, "上游发送失败", string.Empty));

        var result = await fixture.Service.SendAsync(Identity, Mobile, CancellationToken.None);

        Assert.Equal(SendVerificationCodeStatus.Failed, result.Status);
        var log = Assert.Single(fixture.MessageLogs.Added);
        var update = Assert.Single(fixture.MessageLogs.Updates);
        Assert.Equal(log.TransactionId, update.TransactionId);
        Assert.Equal("上游发送失败", update.State);
    }

    [Fact]
    public async Task VerifyAsync_returns_success_marks_the_code_used_and_rejects_reuse()
    {
        var fixture = CreateFixture(
            existing: new VerificationCodeRecord(
                19,
                Identity.CreditCode,
                Mobile,
                "654321",
                Now.AddMinutes(3),
                Now.AddSeconds(-1),
                null));

        var result = await fixture.Service.VerifyAsync(
            Identity,
            Mobile,
            "654321",
            CancellationToken.None);

        Assert.Equal(VerifyVerificationCodeStatus.Success, result.Status);
        Assert.Equal(Now, fixture.Codes.Record?.VerifiedAt);
        Assert.Equal(1, fixture.Codes.MarkVerifiedCallCount);

        var reused = await fixture.Service.VerifyAsync(
            Identity,
            Mobile,
            "654321",
            CancellationToken.None);

        Assert.Equal(VerifyVerificationCodeStatus.Expired, reused.Status);
        Assert.Equal(1, fixture.Codes.MarkVerifiedCallCount);
    }

    [Fact]
    public async Task VerifyAsync_distinguishes_wrong_code_and_expired_code()
    {
        var wrongCodeFixture = CreateFixture(
            existing: new VerificationCodeRecord(
                19,
                Identity.CreditCode,
                Mobile,
                "654321",
                Now.AddMinutes(3),
                Now.AddSeconds(-1),
                null));

        var wrongCode = await wrongCodeFixture.Service.VerifyAsync(
            Identity,
            Mobile,
            "000000",
            CancellationToken.None);
        Assert.Equal(VerifyVerificationCodeStatus.Failed, wrongCode.Status);
        Assert.Equal(0, wrongCodeFixture.Codes.MarkVerifiedCallCount);

        var expiredFixture = CreateFixture(
            existing: new VerificationCodeRecord(
                19,
                Identity.CreditCode,
                Mobile,
                "654321",
                Now.AddSeconds(-1),
                Now.AddSeconds(-30),
                null));

        var expired = await expiredFixture.Service.VerifyAsync(
            Identity,
            Mobile,
            "654321",
            CancellationToken.None);
        Assert.Equal(VerifyVerificationCodeStatus.Expired, expired.Status);
    }

    private static Fixture CreateFixture(
        VerificationCodeRecord? existing = null,
        string code = "123456",
        SmsSendResult? gatewayResult = null)
    {
        var config = new FakeConfig();
        var codes = new FakeCodeRepository(existing);
        var messageLogs = new FakeMessageLogRepository();
        var authentication = new FakeAuthenticationClient(
            new SmsAuthenticationResult(200, true, "success", "mock-auth-token"));
        var gateway = new FakeSmsGateway(
            gatewayResult ?? new SmsSendResult(true, "success", "调用成功!"));
        var codeGenerator = new FakeCodeGenerator(code);
        var transactionIdGenerator = new FakeTransactionIdGenerator();
        var service = new SmsVerificationService(
            config,
            codes,
            messageLogs,
            authentication,
            gateway,
            codeGenerator,
            transactionIdGenerator,
            () => Now);

        return new Fixture(
            service,
            config,
            codes,
            messageLogs,
            authentication,
            gateway,
            codeGenerator);
    }

    private sealed class Fixture
    {
        public Fixture(
            SmsVerificationService service,
            FakeConfig config,
            FakeCodeRepository codes,
            FakeMessageLogRepository messageLogs,
            FakeAuthenticationClient authentication,
            FakeSmsGateway gateway,
            FakeCodeGenerator codeGenerator)
        {
            Service = service;
            Config = config;
            Codes = codes;
            MessageLogs = messageLogs;
            Authentication = authentication;
            Gateway = gateway;
            CodeGenerator = codeGenerator;
        }

        public SmsVerificationService Service { get; }

        public FakeConfig Config { get; }

        public FakeCodeRepository Codes { get; }

        public FakeMessageLogRepository MessageLogs { get; }

        public FakeAuthenticationClient Authentication { get; }

        public FakeSmsGateway Gateway { get; }

        public FakeCodeGenerator CodeGenerator { get; }
    }

    private sealed class FakeConfig : IConfigValueStore
    {
        public FakeConfig()
        {
            Values = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["client_id"] = "client-id",
                ["client_secret"] = "client-secret",
                ["tenant"] = "tenant-a",
                ["zzqscode"] = "sms-code",
                ["zzqssecret"] = "sms-secret"
            };
        }

        public Dictionary<string, string> Values { get; }

        public string ReadRequired(string item) => Values[item];

        public void Set(string item, string value) => Values[item] = value;
    }

    private sealed class FakeCodeRepository : IVerificationCodeRepository
    {
        public FakeCodeRepository(VerificationCodeRecord? record)
        {
            Record = record;
        }

        public VerificationCodeRecord? Record { get; private set; }

        public int SaveCallCount { get; private set; }

        public int MarkVerifiedCallCount { get; private set; }

        public Task<VerificationCodeRecord?> FindByCreditCodeAsync(
            string creditCode,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Record is not null && Record.CreditCode == creditCode ? Record : null);
        }

        public Task SaveAsync(VerificationCodeRecord record, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCallCount++;
            Record = record with { Id = record.Id == 0 ? 19 : record.Id };
            return Task.CompletedTask;
        }

        public Task MarkVerifiedAsync(
            VerificationCodeRecord record,
            DateTimeOffset verifiedAt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MarkVerifiedCallCount++;
            Record = record with { VerifiedAt = verifiedAt };
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMessageLogRepository : IMessageLogRepository
    {
        public List<SmsMessageLogEntry> Added { get; } = new();

        public List<LogUpdate> Updates { get; } = new();

        public Task AddAsync(SmsMessageLogEntry entry, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Added.Add(entry);
            return Task.CompletedTask;
        }

        public Task UpdateStateAsync(
            string transactionId,
            DateTimeOffset date,
            string state,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Updates.Add(new LogUpdate(transactionId, date, state));
            return Task.CompletedTask;
        }
    }

    private sealed record LogUpdate(string TransactionId, DateTimeOffset Date, string State);

    private sealed class FakeAuthenticationClient : ISmsAuthenticationClient
    {
        private readonly SmsAuthenticationResult result;

        public FakeAuthenticationClient(SmsAuthenticationResult result)
        {
            this.result = result;
        }

        public SmsAuthenticationRequest? Request { get; private set; }

        public Task<SmsAuthenticationResult> AuthenticateAsync(
            SmsAuthenticationRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Request = request;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeSmsGateway : ISmsGateway
    {
        private readonly SmsSendResult result;

        public FakeSmsGateway(SmsSendResult result)
        {
            this.result = result;
        }

        public SmsSendRequest? Request { get; private set; }

        public Task<SmsSendResult> SendAsync(
            SmsSendRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Request = request;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeCodeGenerator : IVerificationCodeGenerator
    {
        private readonly string code;

        public FakeCodeGenerator(string code)
        {
            this.code = code;
        }

        public int CallCount { get; private set; }

        public string Generate()
        {
            CallCount++;
            return code;
        }
    }

    private sealed class FakeTransactionIdGenerator : ITransactionIdGenerator
    {
        public string Generate(DateTimeOffset now) => "msg-2026080748-000000001";
    }
}
