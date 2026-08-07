using System.Globalization;
using ForguncyServerApi.Application;
using GrapeCity.Forguncy.ServerApi;

namespace ForguncyServerApi.Infrastructure;

public sealed class ForguncySmsVerificationRepository : IVerificationCodeRepository
{
    private const string VerificationTableName = "enterprise_sms_verification";
    private const string StorageErrorMessage = "The SMS verification storage is invalid.";

    private readonly IDataAccess dataAccess;

    public ForguncySmsVerificationRepository(IDataAccess dataAccess)
    {
        this.dataAccess = dataAccess ?? throw new ArgumentNullException(nameof(dataAccess));
    }

    public Task<VerificationCodeRecord?> FindByCreditCodeAsync(
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
            var row = dataAccess.GetTableData(
                VerificationTableName,
                new ColumnValuePair { ColumnName = "creditcode", Value = creditCode });
            return Task.FromResult(row is null || row.Count == 0 ? null : Map(row));
        }
        catch (Exception)
        {
            throw new InvalidOperationException(StorageErrorMessage);
        }
    }

    public Task SaveAsync(VerificationCodeRecord record, CancellationToken cancellationToken)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var existing = dataAccess.GetTableData(
                VerificationTableName,
                new ColumnValuePair { ColumnName = "creditcode", Value = record.CreditCode });
            var values = new Dictionary<string, object>
            {
                ["creditcode"] = record.CreditCode,
                ["mobile"] = record.Mobile,
                ["code"] = record.Code,
                ["expires_at"] = ToDatabaseDate(record.ExpiresAt),
                ["retry_at"] = ToDatabaseDate(record.RetryAt),
                ["verified_at"] = record.VerifiedAt is null
                    ? null!
                    : ToDatabaseDate(record.VerifiedAt.Value)
            };

            if (existing is null || existing.Count == 0)
            {
                dataAccess.AddTableData(VerificationTableName, values);
            }
            else
            {
                dataAccess.UpdateTableData(
                    VerificationTableName,
                    new ColumnValuePair { ColumnName = "id", Value = ReadId(existing) },
                    values);
            }

            return Task.CompletedTask;
        }
        catch (Exception)
        {
            throw new InvalidOperationException(StorageErrorMessage);
        }
    }

    public Task MarkVerifiedAsync(
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
            dataAccess.UpdateTableData(
                VerificationTableName,
                new ColumnValuePair { ColumnName = "id", Value = record.Id },
                new Dictionary<string, object>
                {
                    ["verified_at"] = ToDatabaseDate(verifiedAt)
                });
            return Task.CompletedTask;
        }
        catch (Exception)
        {
            throw new InvalidOperationException(StorageErrorMessage);
        }
    }

    private static VerificationCodeRecord Map(Dictionary<string, object> row) =>
        new(
            ReadId(row),
            ReadString(row, "creditcode"),
            ReadString(row, "mobile"),
            ReadString(row, "code"),
            ReadDate(row, "expires_at")
                ?? throw new InvalidOperationException(StorageErrorMessage),
            ReadDate(row, "retry_at")
                ?? throw new InvalidOperationException(StorageErrorMessage),
            ReadDate(row, "verified_at"));

    private static long ReadId(Dictionary<string, object> row)
    {
        var value = ReadValue(row, "id");
        try
        {
            return value is null
                ? throw new InvalidOperationException(StorageErrorMessage)
                : Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            throw new InvalidOperationException(StorageErrorMessage);
        }
    }

    private static string ReadString(Dictionary<string, object> row, string name)
    {
        var value = ReadValue(row, name);
        return value is string text && !string.IsNullOrWhiteSpace(text)
            ? text
            : throw new InvalidOperationException(StorageErrorMessage);
    }

    private static object? ReadValue(Dictionary<string, object> row, string name)
    {
        var pair = row.FirstOrDefault(item =>
            string.Equals(item.Key, name, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrEmpty(pair.Key) ? null : pair.Value;
    }

    private static DateTimeOffset? ReadDate(Dictionary<string, object> row, string name)
    {
        var raw = ReadValue(row, name);
        if (raw is null || raw is DBNull)
        {
            return null;
        }

        if (raw is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset;
        }

        if (raw is DateTime dateTime)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Local));
        }

        if (raw is double or float or decimal)
        {
            try
            {
                var oaDate = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
                return new DateTimeOffset(
                    DateTime.SpecifyKind(DateTime.FromOADate(oaDate), DateTimeKind.Local));
            }
            catch (Exception)
            {
                throw new InvalidOperationException(StorageErrorMessage);
            }
        }

        if (raw is string text
            && DateTimeOffset.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException(StorageErrorMessage);
    }

    private static DateTime ToDatabaseDate(DateTimeOffset value) => value.LocalDateTime;
}

public sealed class ForguncyMessageLogRepository : IMessageLogRepository
{
    private const string MessageLogTableName = "m_message_log";
    private const string StorageErrorMessage = "The SMS message log storage is invalid.";

    private readonly IDataAccess dataAccess;

    public ForguncyMessageLogRepository(IDataAccess dataAccess)
    {
        this.dataAccess = dataAccess ?? throw new ArgumentNullException(nameof(dataAccess));
    }

    public Task AddAsync(SmsMessageLogEntry entry, CancellationToken cancellationToken)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            dataAccess.AddTableData(
                MessageLogTableName,
                new Dictionary<string, object>
                {
                    ["sender"] = entry.Sender,
                    ["mobile"] = entry.Mobile,
                    ["content"] = entry.Content,
                    ["reciveder"] = entry.Reciveder,
                    ["transactionID"] = entry.TransactionId
                });
            return Task.CompletedTask;
        }
        catch (Exception)
        {
            throw new InvalidOperationException(StorageErrorMessage);
        }
    }

    public Task UpdateStateAsync(
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
            dataAccess.UpdateTableData(
                MessageLogTableName,
                new ColumnValuePair { ColumnName = "transactionID", Value = transactionId },
                new Dictionary<string, object>
                {
                    ["date"] = date.LocalDateTime,
                    ["state"] = state
                });
            return Task.CompletedTask;
        }
        catch (Exception)
        {
            throw new InvalidOperationException(StorageErrorMessage);
        }
    }
}
