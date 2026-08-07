using System.Collections;
using System.Globalization;
using ForguncyServerApi.Application;
using GrapeCity.Forguncy.ServerApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ForguncyServerApi.Infrastructure;

public sealed class ForguncyConfigValueStore : IConfigValueStore
{
    private const string ConfigTableName = "config";
    private const string InvalidConfigurationMessage =
        "The Forguncy SMS configuration is invalid.";

    private readonly IDataAccess dataAccess;

    public ForguncyConfigValueStore(IDataAccess dataAccess)
    {
        this.dataAccess = dataAccess ?? throw new ArgumentNullException(nameof(dataAccess));
    }

    public string ReadRequired(string item)
    {
        if (string.IsNullOrWhiteSpace(item))
        {
            throw new ArgumentException("A config item is required.", nameof(item));
        }

        try
        {
            var row = ReadRow(item);
            var value = ReadValue(row, "value");
            return value is string text && !string.IsNullOrWhiteSpace(text)
                ? text
                : throw InvalidConfiguration();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception)
        {
            throw InvalidConfiguration();
        }
    }

    public void Set(string item, string value)
    {
        if (string.IsNullOrWhiteSpace(item) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty config item and value are required.");
        }

        try
        {
            var row = ReadRow(item);
            if (row is null || row.Count == 0)
            {
                dataAccess.AddTableData(
                    ConfigTableName,
                    new Dictionary<string, object>
                    {
                        ["item"] = item,
                        ["value"] = value
                    });
                return;
            }

            var id = ReadRowId(row);
            dataAccess.UpdateTableData(
                ConfigTableName,
                new ColumnValuePair { ColumnName = "id", Value = id },
                new Dictionary<string, object> { ["value"] = value });
        }
        catch (Exception)
        {
            throw InvalidConfiguration();
        }
    }

    private Dictionary<string, object>? ReadRow(string item)
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
                return ConvertODataResultToRow(
                    dataAccess.GetTableData(BuildItemODataPath(item)));
            }
            catch (Exception)
            {
                throw InvalidConfiguration();
            }
        }
    }

    private static object? ReadValue(Dictionary<string, object>? row, string name)
    {
        if (row is null)
        {
            return null;
        }

        var pair = row.FirstOrDefault(item =>
            string.Equals(item.Key, name, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrEmpty(pair.Key) ? null : pair.Value;
    }

    private static long ReadRowId(Dictionary<string, object> row)
    {
        var rawId = ReadValue(row, "id");
        if (rawId is null)
        {
            throw InvalidConfiguration();
        }

        try
        {
            return Convert.ToInt64(rawId, CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            throw InvalidConfiguration();
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

        if (result is IEnumerable objectRows)
        {
            foreach (var row in objectRows)
            {
                var converted = ConvertODataResultToRow(row);
                if (converted is not null)
                {
                    return converted;
                }
            }

            return null;
        }

        try
        {
            return ConvertJsonTokenToRow(JToken.Parse(JsonConvert.SerializeObject(result)));
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

    private static InvalidOperationException InvalidConfiguration() =>
        new(InvalidConfigurationMessage);
}
