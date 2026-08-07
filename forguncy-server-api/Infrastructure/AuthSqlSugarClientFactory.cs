using SqlSugar;

namespace ForguncyServerApi.Infrastructure;

public static class AuthSqlSugarClientFactory
{
    public static SqlSugarClient Create(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A connection string is required.", nameof(connectionString));
        }

        return new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = DbType.MySql,
            IsAutoCloseConnection = true
        });
    }
}
