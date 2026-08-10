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

        AssertNestedColumn(enterpriseRow, "Id", "id", isPrimaryKey: true);
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

        var buildLookupQuery = typeof(EnterpriseRepository).GetMethod("BuildLookupQuery", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(buildLookupQuery);

        var query = buildLookupQuery!.Invoke(null, new object[] { client, "91330200SYNTHETIC" });
        Assert.NotNull(query);
        var toSql = query!.GetType().GetMethod("ToSql", Type.EmptyTypes);
        Assert.NotNull(toSql);
        var sqlResult = toSql!.Invoke(query, null);
        Assert.NotNull(sqlResult);
        var keyProperty = sqlResult!.GetType().GetProperty("Key");
        Assert.NotNull(keyProperty);
        var sql = Assert.IsType<string>(keyProperty!.GetValue(sqlResult));

        Assert.Contains("m_preliminary_list", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("yj_regioninfo", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("creditCode", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("enterprise`.`county` = `region`.`id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("enterprise`.`id` AS `UserId", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LandDemand_record_maps_to_the_real_landusedemand_info_columns()
    {
        var table = typeof(LandDemandRecord).GetCustomAttribute<SugarTable>();
        Assert.NotNull(table);
        Assert.Equal("landusedemand_info", table!.TableName);

        AssertLandDemandColumn(nameof(LandDemandRecord.Id), "id", isPrimaryKey: true);
        AssertLandDemandColumn(nameof(LandDemandRecord.County), "county");
        AssertLandDemandColumn(nameof(LandDemandRecord.Region), "region");
        AssertLandDemandColumn(nameof(LandDemandRecord.Businessname), "businessname");
        AssertLandDemandColumn(nameof(LandDemandRecord.Creditcode), "creditcode");
        AssertLandDemandColumn(nameof(LandDemandRecord.Area), "area");
        AssertLandDemandColumn(nameof(LandDemandRecord.BuildingArea), "building_area");
        AssertLandDemandColumn(nameof(LandDemandRecord.ExpectPark), "expect_park");
        AssertLandDemandColumn(nameof(LandDemandRecord.ExpectTime), "expect_time");
        AssertLandDemandColumn(nameof(LandDemandRecord.IsDeploy), "is_deploy");
        AssertLandDemandColumn(nameof(LandDemandRecord.DeployPark), "deploy_park");
        AssertLandDemandColumn(nameof(LandDemandRecord.IsSpecialuse), "is_specialuse");
        AssertLandDemandColumn(nameof(LandDemandRecord.DeployLandtype), "deploy_landtype");
        AssertLandDemandColumn(nameof(LandDemandRecord.DeployHeight), "deploy_height");
        AssertLandDemandColumn(nameof(LandDemandRecord.DeployWeight), "deploy_weight");
        AssertLandDemandColumn(nameof(LandDemandRecord.Investment), "investment");
        AssertLandDemandColumn(nameof(LandDemandRecord.ProjectHydm), "project_hydm");
        AssertLandDemandColumn(nameof(LandDemandRecord.Keyindustry), "keyindustry");
        AssertLandDemandColumn(nameof(LandDemandRecord.Futureindustry), "futureindustry");
        AssertLandDemandColumn(nameof(LandDemandRecord.PredYs), "pred_ys");
        AssertLandDemandColumn(nameof(LandDemandRecord.PredTax), "pred_tax");
        AssertLandDemandColumn(nameof(LandDemandRecord.PredRdex), "pred_rdex");
        AssertLandDemandColumn(nameof(LandDemandRecord.PredUnitenergy), "pred_unitenergy");
        AssertLandDemandColumn(nameof(LandDemandRecord.Projectdata), "projectdata");
        AssertLandDemandColumn(nameof(LandDemandRecord.IsFinancing), "is_financing");
        AssertLandDemandColumn(nameof(LandDemandRecord.FinancingMoney), "financing_money");
        AssertLandDemandColumn(nameof(LandDemandRecord.FinancingTime), "financing_time");
        AssertLandDemandColumn(nameof(LandDemandRecord.Contact), "contact");
        AssertLandDemandColumn(nameof(LandDemandRecord.Office), "office");
        AssertLandDemandColumn(nameof(LandDemandRecord.Phone), "phone");
        AssertLandDemandColumn(nameof(LandDemandRecord.Landusedemand), "landusedemand");
        AssertLandDemandColumn(nameof(LandDemandRecord.Updatetime), "updatetime");
        AssertLandDemandColumn(nameof(LandDemandRecord.Updateuser), "updateuser");
    }

    [Fact]
    public void LandDemand_query_targets_landusedemand_info_and_creditcode()
    {
        using var client = AuthSqlSugarClientFactory.Create("Server=localhost;Database=synthetic;User=root;Password=synthetic;");

        var buildLookupQuery = typeof(LandDemandRepository).GetMethod("BuildLookupQuery", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(buildLookupQuery);

        var query = buildLookupQuery!.Invoke(null, new object[] { client, "91330200SYNTHETIC" });
        Assert.NotNull(query);
        var toSql = query!.GetType().GetMethod("ToSql", Type.EmptyTypes);
        Assert.NotNull(toSql);
        var sqlResult = toSql!.Invoke(query, null);
        Assert.NotNull(sqlResult);
        var keyProperty = sqlResult!.GetType().GetProperty("Key");
        Assert.NotNull(keyProperty);
        var sql = Assert.IsType<string>(keyProperty!.GetValue(sqlResult));

        Assert.Contains("landusedemand_info", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("creditcode", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LandDemand_update_command_sets_only_writable_fields_and_audit_columns()
    {
        using var client = AuthSqlSugarClientFactory.Create("Server=localhost;Database=synthetic;User=root;Password=synthetic;");
        var request = new LandDemandWriteRequest
        {
            Area = "50亩",
            BuildingArea = 1200.50m,
            ExpectPark = "Ningbo Industrial Park",
            ExpectTime = "2026-08",
            IsDeploy = "0",
            DeployPark = null,
            IsSpecialuse = "0",
            DeployLandtype = null,
            DeployHeight = 12.5m,
            DeployWeight = 2.5m,
            Investment = 6000m,
            ProjectHydm = "A0111",
            Keyindustry = "高端装备",
            Futureindustry = "智能制造",
            PredYs = 7000m,
            PredTax = 800m,
            PredRdex = 300m,
            PredUnitenergy = 15m,
            Projectdata = "Build a new production line.",
            IsFinancing = "0",
            FinancingMoney = null,
            FinancingTime = null,
            Contact = "Alice",
            Office = "General Manager",
            Phone = "13800000000",
            Landusedemand = "1"
        };

        var buildUpdateCommand = typeof(LandDemandRepository).GetMethod("BuildUpdateCommand", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(buildUpdateCommand);

        var command = buildUpdateCommand!.Invoke(null, new object[] { client, "91330200SYNTHETIC", request, "2026-08-06 10:20:30", "91330200SYNTHETIC" });
        Assert.NotNull(command);
        var toSql = command!.GetType().GetMethod("ToSql", Type.EmptyTypes);
        Assert.NotNull(toSql);
        var sqlResult = toSql!.Invoke(command, null);
        Assert.NotNull(sqlResult);
        var keyProperty = sqlResult!.GetType().GetProperty("Key");
        Assert.NotNull(keyProperty);
        var sql = Assert.IsType<string>(keyProperty!.GetValue(sqlResult));

        Assert.Contains("landusedemand_info", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("area", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("building_area", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("projectdata", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("updateuser", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("updatetime", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("creditcode", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("businessname", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("county", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("region", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("id` =", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sms_verification_row_maps_to_enterprise_sms_verification_columns()
    {
        var rowType = typeof(SqlSugarVerificationCodeRepository).GetNestedType(
            "VerificationCodeRow",
            BindingFlags.NonPublic);
        Assert.NotNull(rowType);

        var table = rowType!.GetCustomAttribute<SugarTable>();
        Assert.NotNull(table);
        Assert.Equal("enterprise_sms_verification", table!.TableName);

        AssertNestedColumn(rowType, "Id", "id", isPrimaryKey: true);
        AssertNestedColumn(rowType, "CreditCode", "creditcode");
        AssertNestedColumn(rowType, "Mobile", "mobile");
        AssertNestedColumn(rowType, "Code", "code");
        AssertNestedColumn(rowType, "ExpiresAt", "expires_at");
        AssertNestedColumn(rowType, "RetryAt", "retry_at");
        AssertNestedColumn(rowType, "VerifiedAt", "verified_at");
    }

    [Fact]
    public void Sms_verification_lookup_targets_enterprise_sms_verification_and_creditcode()
    {
        using var client = AuthSqlSugarClientFactory.Create(
            "Server=localhost;Database=synthetic;User=root;Password=synthetic;");

        var method = typeof(SqlSugarVerificationCodeRepository).GetMethod(
            "BuildLookupQuery",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var query = method!.Invoke(null, new object[] { client, "91330200SYNTHETIC" });
        Assert.NotNull(query);
        var toSql = query!.GetType().GetMethod("ToSql", Type.EmptyTypes);
        Assert.NotNull(toSql);
        var sqlResult = toSql!.Invoke(query, null);
        Assert.NotNull(sqlResult);
        var keyProperty = sqlResult!.GetType().GetProperty("Key");
        Assert.NotNull(keyProperty);
        var sql = Assert.IsType<string>(keyProperty!.GetValue(sqlResult));

        Assert.Contains("enterprise_sms_verification", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("creditcode", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sms_message_log_rows_map_to_the_required_columns()
    {
        var repositoryType = typeof(SqlSugarMessageLogRepository);
        var insertRowType = repositoryType.GetNestedType(
            "MessageLogInsertRow",
            BindingFlags.NonPublic);
        var stateRowType = repositoryType.GetNestedType(
            "MessageLogStateRow",
            BindingFlags.NonPublic);
        Assert.NotNull(insertRowType);
        Assert.NotNull(stateRowType);

        Assert.Equal(
            "m_message_log",
            insertRowType!.GetCustomAttribute<SugarTable>()!.TableName);
        Assert.Equal(
            "m_message_log",
            stateRowType!.GetCustomAttribute<SugarTable>()!.TableName);

        AssertNestedColumn(insertRowType, "Sender", "sender");
        AssertNestedColumn(insertRowType, "Mobile", "mobile");
        AssertNestedColumn(insertRowType, "Content", "content");
        AssertNestedColumn(insertRowType, "Reciveder", "reciveder");
        AssertNestedColumn(insertRowType, "TransactionId", "transactionID");
        AssertNestedColumn(stateRowType, "TransactionId", "transactionID");
        AssertNestedColumn(stateRowType, "Date", "date");
        AssertNestedColumn(stateRowType, "State", "state");
    }

    [Fact]
    public void Sms_message_log_state_update_targets_transactionID()
    {
        using var client = AuthSqlSugarClientFactory.Create(
            "Server=localhost;Database=synthetic;User=root;Password=synthetic;");

        var method = typeof(SqlSugarMessageLogRepository).GetMethod(
            "BuildUpdateStateCommand",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var command = method!.Invoke(
            null,
            new object[]
            {
                client,
                "msg-2026080748-000000001",
                DateTime.Parse("2026-08-07 12:34:56"),
                "调用成功!"
            });
        Assert.NotNull(command);
        var toSql = command!.GetType().GetMethod("ToSql", Type.EmptyTypes);
        Assert.NotNull(toSql);
        var sqlResult = toSql!.Invoke(command, null);
        Assert.NotNull(sqlResult);
        var keyProperty = sqlResult!.GetType().GetProperty("Key");
        Assert.NotNull(keyProperty);
        var sql = Assert.IsType<string>(keyProperty!.GetValue(sqlResult));

        Assert.Contains("m_message_log", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("transactionID", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("date", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("state", sql, StringComparison.OrdinalIgnoreCase);
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

    private static void AssertNestedColumn(Type rowType, string propertyName, string columnName, bool isPrimaryKey = false)
    {
        var property = rowType.GetProperty(propertyName);
        Assert.NotNull(property);

        var column = property!.GetCustomAttribute<SugarColumn>();
        Assert.NotNull(column);
        Assert.Equal(columnName, column!.ColumnName);
        Assert.Equal(isPrimaryKey, column.IsPrimaryKey);
    }

    private static void AssertLandDemandColumn(string propertyName, string columnName, bool isPrimaryKey = false)
    {
        var property = typeof(LandDemandRecord).GetProperty(propertyName);
        Assert.NotNull(property);

        var column = property!.GetCustomAttribute<SugarColumn>();
        Assert.NotNull(column);
        Assert.Equal(columnName, column!.ColumnName);
        Assert.Equal(isPrimaryKey, column.IsPrimaryKey);
    }
}
