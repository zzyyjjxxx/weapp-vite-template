using GrapeCity.Forguncy.ServerApi;

namespace ForguncyServerApi.Infrastructure;

public static class ForguncyConfigConnectionStringReader
{
    private const string InvalidConfigurationMessage =
        "The Forguncy authentication connection configuration is invalid.";

    public static string ReadRequired(IDataAccess dataAccess)
    {
        if (dataAccess is null)
        {
            throw new ArgumentNullException(nameof(dataAccess));
        }

        Dictionary<string, object>? row;
        try
        {
            row = dataAccess.GetTableData(
                "config",
                new ColumnValuePair { ColumnName = "item", Value = "ssl" });
        }
        catch (Exception)
        {
            throw new InvalidOperationException(InvalidConfigurationMessage);
        }

        if (row is null
            || !row.TryGetValue("value", out var value)
            || value is not string connectionString
            || string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(InvalidConfigurationMessage);
        }

        return connectionString;
    }
}
