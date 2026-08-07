using System.Reflection;
using ForguncyServerApi.Domain;
using ForguncyServerApi.Infrastructure;
using SqlSugar;
using Xunit;

namespace ForguncyServerApi.Tests.Infrastructure;

public sealed class SqlSugarPersistenceTests
{
    [Fact]
    public void AuthUser_maps_to_the_real_c_userinfo_columns()
    {
        var table = typeof(AuthUser).GetCustomAttribute<SugarTable>();
        Assert.NotNull(table);
        Assert.Equal("c_userinfo", table!.TableName);

        AssertColumn(nameof(AuthUser.Id), "id", isPrimaryKey: true);
        AssertColumn(nameof(AuthUser.Username), "creditCode");
        AssertColumn(nameof(AuthUser.PasswordHash), "password");
        AssertColumn(nameof(AuthUser.IsOpen), "isopen");
    }

    [Fact]
    public void SqlSugar_client_factory_uses_mysql_and_auto_close()
    {
        using var client = AuthSqlSugarClientFactory.Create("Server=localhost;Database=synthetic;User=root;Password=synthetic;");

        Assert.Equal(DbType.MySql, client.CurrentConnectionConfig.DbType);
        Assert.True(client.CurrentConnectionConfig.IsAutoCloseConnection);
    }

    [Fact]
    public void User_query_targets_c_userinfo_and_creditCode()
    {
        using var client = AuthSqlSugarClientFactory.Create("Server=localhost;Database=synthetic;User=root;Password=synthetic;");

        var sql = client.Queryable<AuthUser>()
            .Where(user => user.Username == "91330200SYNTHETIC")
            .ToSql();

        Assert.Contains("c_userinfo", sql.Key, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("creditCode", sql.Key, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertColumn(string propertyName, string columnName, bool isPrimaryKey = false)
    {
        var property = typeof(AuthUser).GetProperty(propertyName);
        Assert.NotNull(property);

        var column = property!.GetCustomAttribute<SugarColumn>();
        Assert.NotNull(column);
        Assert.Equal(columnName, column!.ColumnName);
        Assert.Equal(isPrimaryKey, column.IsPrimaryKey);
    }
}
