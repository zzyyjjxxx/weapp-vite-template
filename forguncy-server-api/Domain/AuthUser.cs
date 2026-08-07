using SqlSugar;

namespace ForguncyServerApi.Domain;

[SugarTable("c_userinfo")]
public sealed class AuthUser
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(ColumnName = "creditCode")]
    public string Username { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "password")]
    public string PasswordHash { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "isopen")]
    public int IsOpen { get; set; }
}
