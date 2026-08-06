using System.Reflection;
using ForguncyServerApi.Application;
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

    [Fact]
    public void Enterprise_row_maps_to_the_real_m_preliminary_list_columns()
    {
        var enterpriseRow = typeof(EnterpriseRepository).GetNestedType("EnterpriseRow", BindingFlags.NonPublic);
        Assert.NotNull(enterpriseRow);

        var table = enterpriseRow!.GetCustomAttribute<SugarTable>();
        Assert.NotNull(table);
        Assert.Equal("m_preliminary_list", table!.TableName);

        AssertNestedColumn(enterpriseRow, "BusinessName", "businessName");
        AssertNestedColumn(enterpriseRow, "CreditCode", "creditCode");
        AssertNestedColumn(enterpriseRow, "CountyId", "county");
        AssertNestedColumn(enterpriseRow, "Region", "region");
    }

    [Fact]
    public void Region_row_maps_to_the_real_yj_regioninfo_columns()
    {
        var regionRow = typeof(EnterpriseRepository).GetNestedType("RegionRow", BindingFlags.NonPublic);
        Assert.NotNull(regionRow);

        var table = regionRow!.GetCustomAttribute<SugarTable>();
        Assert.NotNull(table);
        Assert.Equal("yj_regioninfo", table!.TableName);

        AssertNestedColumn(regionRow, "Id", "id");
        AssertNestedColumn(regionRow, "Name", "name");
    }

    [Fact]
    public void Enterprise_query_joins_regioninfo_by_county_and_filters_by_creditCode()
    {
        using var client = AuthSqlSugarClientFactory.Create("Server=localhost;Database=synthetic;User=root;Password=synthetic;");

        var buildSql = typeof(EnterpriseRepository).GetMethod("BuildLookupSql", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(buildSql);

        var sql = Assert.IsType<string>(buildSql!.Invoke(null, new object[] { client, "91330200SYNTHETIC" }));

        Assert.Contains("m_preliminary_list", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("yj_regioninfo", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("creditCode", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("yj_regioninfo.id", sql, StringComparison.OrdinalIgnoreCase);
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

    private static void AssertNestedColumn(Type rowType, string propertyName, string columnName)
    {
        var property = rowType.GetProperty(propertyName);
        Assert.NotNull(property);

        var column = property!.GetCustomAttribute<SugarColumn>();
        Assert.NotNull(column);
        Assert.Equal(columnName, column!.ColumnName);
    }
}
