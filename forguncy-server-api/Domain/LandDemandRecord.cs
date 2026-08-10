using SqlSugar;

namespace ForguncyServerApi.Domain;

[SugarTable("landusedemand_info")]
public sealed class LandDemandRecord
{
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public int Id { get; set; }

    [SugarColumn(ColumnName = "county")]
    public string County { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "region")]
    public string Region { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "businessname")]
    public string Businessname { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "creditcode")]
    public string Creditcode { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "area")]
    public string? Area { get; set; }

    [SugarColumn(ColumnName = "building_area")]
    public decimal? BuildingArea { get; set; }

    [SugarColumn(ColumnName = "expect_park")]
    public string? ExpectPark { get; set; }

    [SugarColumn(ColumnName = "expect_time")]
    public string? ExpectTime { get; set; }

    [SugarColumn(ColumnName = "is_deploy")]
    public string? IsDeploy { get; set; }

    [SugarColumn(ColumnName = "deploy_park")]
    public string? DeployPark { get; set; }

    [SugarColumn(ColumnName = "is_specialuse")]
    public string? IsSpecialuse { get; set; }

    [SugarColumn(ColumnName = "deploy_landtype")]
    public string? DeployLandtype { get; set; }

    [SugarColumn(ColumnName = "deploy_height")]
    public decimal? DeployHeight { get; set; }

    [SugarColumn(ColumnName = "deploy_weight")]
    public decimal? DeployWeight { get; set; }

    [SugarColumn(ColumnName = "investment")]
    public decimal? Investment { get; set; }

    [SugarColumn(ColumnName = "project_hydm")]
    public string? ProjectHydm { get; set; }

    [SugarColumn(ColumnName = "keyindustry")]
    public string? Keyindustry { get; set; }

    [SugarColumn(ColumnName = "futureindustry")]
    public string? Futureindustry { get; set; }

    [SugarColumn(ColumnName = "pred_ys")]
    public decimal? PredYs { get; set; }

    [SugarColumn(ColumnName = "pred_tax")]
    public decimal? PredTax { get; set; }

    [SugarColumn(ColumnName = "pred_rdex")]
    public decimal? PredRdex { get; set; }

    [SugarColumn(ColumnName = "pred_unitenergy")]
    public decimal? PredUnitenergy { get; set; }

    [SugarColumn(ColumnName = "projectdata")]
    public string? Projectdata { get; set; }

    [SugarColumn(ColumnName = "is_financing")]
    public string? IsFinancing { get; set; }

    [SugarColumn(ColumnName = "financing_money")]
    public decimal? FinancingMoney { get; set; }

    [SugarColumn(ColumnName = "financing_time")]
    public string? FinancingTime { get; set; }

    [SugarColumn(ColumnName = "contact")]
    public string? Contact { get; set; }

    [SugarColumn(ColumnName = "office")]
    public string? Office { get; set; }

    [SugarColumn(ColumnName = "phone")]
    public string? Phone { get; set; }

    [SugarColumn(ColumnName = "landusedemand")]
    public string? Landusedemand { get; set; }

    [SugarColumn(ColumnName = "updatetime")]
    public string? Updatetime { get; set; }

    [SugarColumn(ColumnName = "updateuser")]
    public string? Updateuser { get; set; }
}
