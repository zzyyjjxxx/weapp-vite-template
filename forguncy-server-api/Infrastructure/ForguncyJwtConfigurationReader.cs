using System.Globalization;
using System.Security.Cryptography;
using GrapeCity.Forguncy.ServerApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ForguncyServerApi.Infrastructure;

public static class ForguncyJwtConfigurationReader
{
    private const string ConfigTableName = "config";
    private const string SigningKeyItem = "FGC_JWT_SIGNING_KEY";
    private const string IssuerItem = "FGC_JWT_ISSUER";
    private const string LifetimeItem = "FGC_JWT_EXPIRES_MINUTES";
    private const string RefreshLifetimeItem = "FGC_JWT_REFRESH_EXPIRES_MINUTES";
    private const string DefaultLifetimeMinutes = "60";
    private const string DefaultRefreshLifetimeMinutes = "10080";
    private const string IssuerPrefix = "forguncy-server-api-";
    private const string InvalidConfigurationMessage =
        "The Forguncy JWT configuration is invalid.";

    private static readonly object InitializationLock = new();

    public static IReadOnlyDictionary<string, string?> ReadOrCreate(IDataAccess dataAccess)
    {
        if (dataAccess is null)
        {
            throw new ArgumentNullException(nameof(dataAccess));
        }

        lock (InitializationLock)
        {
            var values = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [SigningKeyItem] = ReadOrCreateValue(dataAccess, SigningKeyItem, GenerateSigningKey),
                [IssuerItem] = ReadOrCreateValue(dataAccess, IssuerItem, GenerateIssuer),
                [LifetimeItem] = ReadOrCreateValue(dataAccess, LifetimeItem, () => DefaultLifetimeMinutes),
                [RefreshLifetimeItem] = ReadOrCreateValue(
                    dataAccess,
                    RefreshLifetimeItem,
                    () => DefaultRefreshLifetimeMinutes)
            };

            return values;
        }
    }

    private static string ReadOrCreateValue(
        IDataAccess dataAccess,
        string item,
        Func<string> valueFactory)
    {
        var row = ReadConfigRow(dataAccess, item);

        if (row is null || row.Count == 0)
        {
            var generatedValue = valueFactory();
            try
            {
                dataAccess.AddTableData(
                    ConfigTableName,
                    new Dictionary<string, object>
                    {
                        ["item"] = item,
                        ["value"] = generatedValue
                    });
            }
            catch (Exception)
            {
                throw InvalidConfiguration();
            }

            return generatedValue;
        }

        if (row.TryGetValue("value", out var rawValue)
            && rawValue is not null
            && rawValue is not DBNull
            && rawValue is not string)
        {
            throw InvalidConfiguration();
        }

        var existingValue = rawValue as string;
        if (!string.IsNullOrWhiteSpace(existingValue))
        {
            return existingValue!;
        }

        var replacementValue = valueFactory();
        var rowId = ReadRowId(row);
        try
        {
            dataAccess.UpdateTableData(
                ConfigTableName,
                new ColumnValuePair { ColumnName = "id", Value = rowId },
                new Dictionary<string, object> { ["value"] = replacementValue });
        }
        catch (Exception)
        {
            throw InvalidConfiguration();
        }

        return replacementValue;
    }

    private static Dictionary<string, object>? ReadConfigRow(IDataAccess dataAccess, string item)
    {
        try
        {
            return dataAccess.GetTableData(
                ConfigTableName,
                new ColumnValuePair { ColumnName = "item", Value = item });
        }
        catch (Exception)
        {
            try
            {
                var result = dataAccess.GetTableData(BuildItemODataPath(item));
                return ConvertODataResultToRow(result);
            }
            catch (Exception)
            {
                throw InvalidConfiguration();
            }
        }
    }

    private static string BuildItemODataPath(string item) =>
        ConfigTableName + "?$filter=item eq '" + item.Replace("'", "''") + "'&$top=1";

    private static Dictionary<string, object>? ConvertODataResultToRow(object? result)
    {
        if (result is null)
        {
            return null;
        }

        if (result is Dictionary<string, object> dictionary)
        {
            return dictionary;
        }

        if (result is IDictionary<string, object> dictionaryInterface)
        {
            return new Dictionary<string, object>(dictionaryInterface, StringComparer.OrdinalIgnoreCase);
        }

        if (result is IEnumerable<Dictionary<string, object>> rows)
        {
            return rows.FirstOrDefault();
        }

        if (result is IEnumerable<object> objectRows)
        {
            var firstRow = objectRows.FirstOrDefault(row => row is not null);
            if (firstRow is not null)
            {
                return ConvertODataResultToRow(firstRow);
            }
        }

        try
        {
            var token = JToken.Parse(JsonConvert.SerializeObject(result));
            return ConvertJsonTokenToRow(token);
        }
        catch (Exception)
        {
            throw InvalidConfiguration();
        }
    }

    private static Dictionary<string, object>? ConvertJsonTokenToRow(JToken token)
    {
        if (token.Type == JTokenType.Null)
        {
            return null;
        }

        if (token is JArray array)
        {
            return array
                .Select(ConvertJsonTokenToRow)
                .FirstOrDefault(row => row is not null);
        }

        if (token is not JObject objectToken)
        {
            throw InvalidConfiguration();
        }

        if (objectToken["value"] is JArray valueArray)
        {
            return ConvertJsonTokenToRow(valueArray);
        }

        if (objectToken["d"] is JObject dObject
            && dObject["results"] is JArray resultArray)
        {
            return ConvertJsonTokenToRow(resultArray);
        }

        var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in objectToken.Properties())
        {
            row[property.Name] = property.Value.Type == JTokenType.Null
                ? null!
                : property.Value.ToObject<object>()!;
        }

        return row;
    }

    private static int ReadRowId(Dictionary<string, object> row)
    {
        if (!row.TryGetValue("id", out var rawId) || rawId is null)
        {
            throw InvalidConfiguration();
        }

        try
        {
            return Convert.ToInt32(rawId, CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            throw InvalidConfiguration();
        }
    }

    private static string GenerateSigningKey()
    {
        var bytes = new byte[32];
        using (var random = RandomNumberGenerator.Create())
        {
            random.GetBytes(bytes);
        }

        return Convert.ToBase64String(bytes);
    }

    private static string GenerateIssuer() =>
        IssuerPrefix + Guid.NewGuid().ToString("N").ToLowerInvariant();

    private static InvalidOperationException InvalidConfiguration() =>
        new(InvalidConfigurationMessage);
}
