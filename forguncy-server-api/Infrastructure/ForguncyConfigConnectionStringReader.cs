using GrapeCity.Forguncy.ServerApi;

namespace ForguncyServerApi.Infrastructure;

public static class ForguncyConfigConnectionStringReader
{
    private const string InvalidConfigurationMessage =
        "The Forguncy authentication connection configuration is invalid.";

    public static string ReadRequired(IDataAccess dataAccess)
    {
        ArgumentNullException.ThrowIfNull(dataAccess);

        var row = dataAccess.GetTableData(
            "config",
            new ColumnValuePair { ColumnName = "item", Value = "ssl" });

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
