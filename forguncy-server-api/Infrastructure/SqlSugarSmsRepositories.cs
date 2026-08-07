using ForguncyServerApi.Application;
using SqlSugar;

namespace ForguncyServerApi.Infrastructure;

public sealed class SqlSugarVerificationCodeRepository : IVerificationCodeRepository
{
    private const string VerificationTableName = "enterprise_sms_verification";
    private const string StorageErrorMessage = "The SMS verification storage is invalid.";

    private readonly Func<SqlSugarClient> clientFactory;

    public SqlSugarVerificationCodeRepository(Func<SqlSugarClient> clientFactory)
    {
        this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    }

    public async Task<VerificationCodeRecord?> FindByCreditCodeAsync(
        string creditCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(creditCode))
        {
            throw new ArgumentException("Credit code is required.", nameof(creditCode));
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var client = clientFactory();
            var row = await BuildLookupQuery(client, creditCode.Trim()).FirstAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return row is null ? null : Map(row);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new InvalidOperationException(StorageErrorMessage);
        }
    }

    public async Task SaveAsync(
        VerificationCodeRecord record,
        CancellationToken cancellationToken)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var client = clientFactory();
            var existing = await BuildLookupQuery(client, record.CreditCode).FirstAsync();
            var row = ToRow(record);

            if (existing is null)
            {
                await client.Insertable(row).ExecuteCommandAsync();
            }
            else
            {
                await BuildUpdateCommand(client, existing.Id, row).ExecuteCommandAsync();
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new InvalidOperationException(StorageErrorMessage);
        }
    }

    public async Task MarkVerifiedAsync(
        VerificationCodeRecord record,
        DateTimeOffset verifiedAt,
        CancellationToken cancellationToken)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var client = clientFactory();
            await client.Updateable<VerificationCodeRow>()
                .SetColumns(row => new VerificationCodeRow
                {
                    VerifiedAt = ToDatabaseDate(verifiedAt)
                })
                .Where(row => row.Id == record.Id)
                .ExecuteCommandAsync();
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new InvalidOperationException(StorageErrorMessage);
        }
    }

    private static ISugarQueryable<VerificationCodeRow> BuildLookupQuery(
        SqlSugarClient client,
        string creditCode) =>
        client.Queryable<VerificationCodeRow>()
            .Where(row => row.CreditCode == creditCode);

    private static IUpdateable<VerificationCodeRow> BuildUpdateCommand(
        SqlSugarClient client,
        long id,
        VerificationCodeRow row) =>
        client.Updateable<VerificationCodeRow>()
            .SetColumns(current => new VerificationCodeRow
            {
                CreditCode = row.CreditCode,
                Mobile = row.Mobile,
                Code = row.Code,
                ExpiresAt = row.ExpiresAt,
                RetryAt = row.RetryAt,
                VerifiedAt = row.VerifiedAt
            })
            .Where(current => current.Id == id);

    private static VerificationCodeRow ToRow(VerificationCodeRecord record) =>
        new()
        {
            Id = record.Id,
            CreditCode = record.CreditCode,
            Mobile = record.Mobile,
            Code = record.Code,
            ExpiresAt = ToDatabaseDate(record.ExpiresAt),
            RetryAt = ToDatabaseDate(record.RetryAt),
            VerifiedAt = record.VerifiedAt is null
                ? null
                : ToDatabaseDate(record.VerifiedAt.Value)
        };

    private static VerificationCodeRecord Map(VerificationCodeRow row) =>
        new(
            row.Id,
            row.CreditCode,
            row.Mobile,
            row.Code,
            ToDateTimeOffset(row.ExpiresAt),
            ToDateTimeOffset(row.RetryAt),
            row.VerifiedAt is null ? null : ToDateTimeOffset(row.VerifiedAt.Value));

    private static DateTime ToDatabaseDate(DateTimeOffset value) => value.LocalDateTime;

    private static DateTimeOffset ToDateTimeOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Local));

    [SugarTable(VerificationTableName)]
    private sealed class VerificationCodeRow
    {
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        [SugarColumn(ColumnName = "creditcode")]
        public string CreditCode { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "mobile")]
        public string Mobile { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "code")]
        public string Code { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "expires_at")]
        public DateTime ExpiresAt { get; set; }

        [SugarColumn(ColumnName = "retry_at")]
        public DateTime RetryAt { get; set; }

        [SugarColumn(ColumnName = "verified_at", IsNullable = true)]
        public DateTime? VerifiedAt { get; set; }
    }
}

public sealed class SqlSugarMessageLogRepository : IMessageLogRepository
{
    private const string MessageLogTableName = "m_message_log";
    private const string StorageErrorMessage = "The SMS message log storage is invalid.";

    private readonly Func<SqlSugarClient> clientFactory;

    public SqlSugarMessageLogRepository(Func<SqlSugarClient> clientFactory)
    {
        this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    }

    public async Task AddAsync(
        SmsMessageLogEntry entry,
        CancellationToken cancellationToken)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var client = clientFactory();
            await client.Insertable(new MessageLogInsertRow
            {
                Sender = entry.Sender,
                Mobile = entry.Mobile,
                Content = entry.Content,
                Reciveder = entry.Reciveder,
                TransactionId = entry.TransactionId
            }).ExecuteCommandAsync();
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new InvalidOperationException(StorageErrorMessage);
        }
    }

    public async Task UpdateStateAsync(
        string transactionId,
        DateTimeOffset date,
        string state,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Transaction id and state are required.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var client = clientFactory();
            await BuildUpdateStateCommand(client, transactionId, date.LocalDateTime, state)
                .ExecuteCommandAsync();
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new InvalidOperationException(StorageErrorMessage);
        }
    }

    private static IUpdateable<MessageLogStateRow> BuildUpdateStateCommand(
        SqlSugarClient client,
        string transactionId,
        DateTime date,
        string state) =>
        client.Updateable<MessageLogStateRow>()
            .SetColumns(row => new MessageLogStateRow
            {
                Date = date,
                State = state
            })
            .Where(row => row.TransactionId == transactionId);

    [SugarTable(MessageLogTableName)]
    private sealed class MessageLogInsertRow
    {
        [SugarColumn(ColumnName = "sender")]
        public string Sender { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "mobile")]
        public string Mobile { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "content")]
        public string Content { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "reciveder")]
        public string Reciveder { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "transactionID")]
        public string TransactionId { get; set; } = string.Empty;
    }

    [SugarTable(MessageLogTableName)]
    private sealed class MessageLogStateRow
    {
        [SugarColumn(ColumnName = "transactionID")]
        public string TransactionId { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "date")]
        public DateTime Date { get; set; }

        [SugarColumn(ColumnName = "state")]
        public string State { get; set; } = string.Empty;
    }
}
