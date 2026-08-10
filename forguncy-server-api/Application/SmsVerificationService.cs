using System.Globalization;

namespace ForguncyServerApi.Application;

public sealed record SmsAuthenticationRequest(
    string ClientId,
    string ClientSecret,
    string Tenant);

public sealed record SmsAuthenticationResult(
    int Code,
    bool Success,
    string Message,
    string? Data);

public sealed record SmsSendRequest(
    string Mobile,
    string Content,
    string TransactionId,
    string Zzqscode,
    string Zzqssecret,
    string AuthToken);

public sealed record SmsSendResult(
    bool Success,
    string Message,
    string RetMessage);

public sealed record VerificationCodeRecord(
    long Id,
    string CreditCode,
    string Mobile,
    string Code,
    DateTimeOffset ExpiresAt,
    DateTimeOffset RetryAt,
    DateTimeOffset? VerifiedAt);

public sealed record SmsMessageLogEntry(
    string Sender,
    string Mobile,
    string Content,
    string Reciveder,
    string TransactionId);

public enum SendVerificationCodeStatus
{
    Success,
    Cooldown,
    Failed
}

public sealed record SendVerificationCodeResult(
    SendVerificationCodeStatus Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset RetryAt);

public enum VerifyVerificationCodeStatus
{
    Success,
    Failed,
    Expired
}

public sealed record VerifyVerificationCodeResult(VerifyVerificationCodeStatus Status);

public interface IConfigValueStore
{
    string ReadRequired(string item);

    void Set(string item, string value);
}

public interface IVerificationCodeRepository
{
    Task<VerificationCodeRecord?> FindByCreditCodeAsync(
        string creditCode,
        CancellationToken cancellationToken);

    Task SaveAsync(VerificationCodeRecord record, CancellationToken cancellationToken);

    Task MarkVerifiedAsync(
        VerificationCodeRecord record,
        DateTimeOffset verifiedAt,
        CancellationToken cancellationToken);
}

public interface IMessageLogRepository
{
    Task AddAsync(SmsMessageLogEntry entry, CancellationToken cancellationToken);

    Task UpdateStateAsync(
        string transactionId,
        DateTimeOffset date,
        string state,
        CancellationToken cancellationToken);
}

public interface ISmsAuthenticationClient
{
    Task<SmsAuthenticationResult> AuthenticateAsync(
        SmsAuthenticationRequest request,
        CancellationToken cancellationToken);
}

public interface ISmsGateway
{
    Task<SmsSendResult> SendAsync(
        SmsSendRequest request,
        CancellationToken cancellationToken);
}

public interface IVerificationCodeGenerator
{
    string Generate();
}

public interface ITransactionIdGenerator
{
    string Generate(DateTimeOffset now);
}

public sealed class SmsIntegrationException : Exception
{
    public SmsIntegrationException(string message)
        : base(message)
    {
    }
}

public sealed class SmsVerificationService
{
    private const string ClientIdItem = "client_id";
    private const string ClientSecretItem = "client_secret";
    private const string TenantItem = "tenant";
    private const string TokenItem = "token";
    private const string ZzqscodeItem = "zzqscode";
    private const string ZzqssecretItem = "zzqssecret";
    private const int CodeExpiryMinutes = 5;
    private const int RetryDelaySeconds = 60;

    private readonly IConfigValueStore config;
    private readonly IVerificationCodeRepository verificationCodes;
    private readonly IMessageLogRepository messageLogs;
    private readonly ISmsAuthenticationClient authenticationClient;
    private readonly ISmsGateway smsGateway;
    private readonly IVerificationCodeGenerator codeGenerator;
    private readonly ITransactionIdGenerator transactionIdGenerator;
    private readonly Func<DateTimeOffset> clock;

    public SmsVerificationService(
        IConfigValueStore config,
        IVerificationCodeRepository verificationCodes,
        IMessageLogRepository messageLogs,
        ISmsAuthenticationClient authenticationClient,
        ISmsGateway smsGateway,
        IVerificationCodeGenerator codeGenerator,
        ITransactionIdGenerator transactionIdGenerator,
        Func<DateTimeOffset> clock)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.verificationCodes = verificationCodes
            ?? throw new ArgumentNullException(nameof(verificationCodes));
        this.messageLogs = messageLogs ?? throw new ArgumentNullException(nameof(messageLogs));
        this.authenticationClient = authenticationClient
            ?? throw new ArgumentNullException(nameof(authenticationClient));
        this.smsGateway = smsGateway ?? throw new ArgumentNullException(nameof(smsGateway));
        this.codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
        this.transactionIdGenerator = transactionIdGenerator
            ?? throw new ArgumentNullException(nameof(transactionIdGenerator));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<SendVerificationCodeResult> SendAsync(
        EnterpriseIdentity identity,
        string mobile,
        CancellationToken cancellationToken)
    {
        ValidateIdentity(identity);
        mobile = NormalizeMobile(mobile);
        cancellationToken.ThrowIfCancellationRequested();

        var authentication = await authenticationClient.AuthenticateAsync(
            new SmsAuthenticationRequest(
                config.ReadRequired(ClientIdItem),
                config.ReadRequired(ClientSecretItem),
                config.ReadRequired(TenantItem)),
            cancellationToken);
        if (authentication.Code != 200
            || !authentication.Success
            || string.IsNullOrWhiteSpace(authentication.Data))
        {
            throw new SmsIntegrationException("SMS authentication failed.");
        }

        config.Set(TokenItem, authentication.Data!);

        var zzqscode = config.ReadRequired(ZzqscodeItem);
        var zzqssecret = config.ReadRequired(ZzqssecretItem);
        var now = clock();
        var existing = await verificationCodes.FindByCreditCodeAsync(
            identity.CreditCode,
            cancellationToken);

        var active = existing is not null
            && existing.VerifiedAt is null
            && existing.ExpiresAt > now
            && string.Equals(existing.Mobile, mobile, StringComparison.Ordinal);
        if (active && existing!.RetryAt > now)
        {
            return new(SendVerificationCodeStatus.Cooldown, existing.ExpiresAt, existing.RetryAt);
        }

        var code = active ? existing!.Code : codeGenerator.Generate();
        if (code.Length != 6 || code.Any(character => character is < '0' or > '9'))
        {
            throw new InvalidOperationException("The generated verification code is invalid.");
        }

        var expiresAt = active ? existing!.ExpiresAt : now.AddMinutes(CodeExpiryMinutes);
        var retryAt = now.AddSeconds(RetryDelaySeconds);
        var record = new VerificationCodeRecord(
            existing?.Id ?? 0,
            identity.CreditCode,
            mobile,
            code,
            expiresAt,
            retryAt,
            null);
        await verificationCodes.SaveAsync(record, cancellationToken);

        var content = $"您好，您的用地需求填报验证码是：【{code}】";
        var transactionId = transactionIdGenerator.Generate(now);
        await messageLogs.AddAsync(
            new SmsMessageLogEntry("system", mobile, content, "企业", transactionId),
            cancellationToken);

        SmsSendResult sendResult;
        try
        {
            sendResult = await smsGateway.SendAsync(
                new SmsSendRequest(
                    mobile,
                    content,
                    transactionId,
                    zzqscode,
                    zzqssecret,
                    authentication.Data!),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            await messageLogs.UpdateStateAsync(
                transactionId,
                clock(),
                "短信发送失败",
                cancellationToken);
            return new(SendVerificationCodeStatus.Failed, expiresAt, retryAt);
        }

        await messageLogs.UpdateStateAsync(
            transactionId,
            clock(),
            sendResult.Success ? sendResult.RetMessage : sendResult.Message,
            cancellationToken);

        return new(
            sendResult.Success ? SendVerificationCodeStatus.Success : SendVerificationCodeStatus.Failed,
            expiresAt,
            retryAt);
    }

    public async Task<VerifyVerificationCodeResult> VerifyAsync(
        EnterpriseIdentity identity,
        string mobile,
        string code,
        CancellationToken cancellationToken)
    {
        ValidateIdentity(identity);
        mobile = NormalizeMobile(mobile);
        if (string.IsNullOrWhiteSpace(code)
            || code.Length != 6
            || code.Any(character => character is < '0' or > '9'))
        {
            return new(VerifyVerificationCodeStatus.Failed);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var now = clock();
        var existing = await verificationCodes.FindByCreditCodeAsync(
            identity.CreditCode,
            cancellationToken);
        if (existing is null
            || existing.VerifiedAt is not null
            || existing.ExpiresAt <= now)
        {
            return new(VerifyVerificationCodeStatus.Expired);
        }

        if (!string.Equals(existing.Mobile, mobile, StringComparison.Ordinal)
            || !string.Equals(existing.Code, code, StringComparison.Ordinal))
        {
            return new(VerifyVerificationCodeStatus.Failed);
        }

        await verificationCodes.MarkVerifiedAsync(existing, now, cancellationToken);
        return new(VerifyVerificationCodeStatus.Success);
    }

    private static void ValidateIdentity(EnterpriseIdentity identity)
    {
        if (identity is null)
        {
            throw new ArgumentNullException(nameof(identity));
        }

        if (identity.UserId <= 0 || string.IsNullOrWhiteSpace(identity.CreditCode))
        {
            throw new ArgumentException("Enterprise identity is invalid.", nameof(identity));
        }
    }

    private static string NormalizeMobile(string mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile))
        {
            throw new ArgumentException("Mobile is required.", nameof(mobile));
        }

        var normalized = mobile.Trim();
        var valid = normalized.Length == 11
            && normalized[0] == '1'
            && normalized[1] is >= '3' and <= '9'
            && normalized.All(char.IsDigit);
        return valid
            ? normalized
            : throw new ArgumentException("Mobile is invalid.", nameof(mobile));
    }
}

public sealed class RandomVerificationCodeGenerator : IVerificationCodeGenerator
{
    public string Generate()
    {
        using var random = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[4];
        random.GetBytes(bytes);
        var value = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
        return value.ToString("D6", CultureInfo.InvariantCulture);
    }
}

public sealed class SequentialTransactionIdGenerator : ITransactionIdGenerator
{
    private static int sequence;

    public string Generate(DateTimeOffset now)
    {
        var next = System.Threading.Interlocked.Increment(ref sequence);
        if (next > 999_999_999)
        {
            System.Threading.Interlocked.Exchange(ref sequence, 1);
            next = 1;
        }

        return $"msg-{now:yyyyMMddss}-{next:D9}";
    }
}
