namespace ForguncyServerApi.Application;

public sealed record LandDemandWriteRequest
{
    public string? Area { get; init; }
    public decimal? BuildingArea { get; init; }
    public string? ExpectPark { get; init; }
    public string? ExpectTime { get; init; }
    public string? IsDeploy { get; init; }
    public string? DeployPark { get; init; }
    public string? IsSpecialuse { get; init; }
    public string? DeployLandtype { get; init; }
    public decimal? DeployHeight { get; init; }
    public decimal? DeployWeight { get; init; }
    public decimal? Investment { get; init; }
    public string? ProjectHydm { get; init; }
    public string? Keyindustry { get; init; }
    public string? Futureindustry { get; init; }
    public decimal? PredYs { get; init; }
    public decimal? PredTax { get; init; }
    public decimal? PredRdex { get; init; }
    public decimal? PredUnitenergy { get; init; }
    public string? Projectdata { get; init; }
    public string? IsFinancing { get; init; }
    public decimal? FinancingMoney { get; init; }
    public string? FinancingTime { get; init; }
    public string? Contact { get; init; }
    public string? Office { get; init; }
    public string? Phone { get; init; }
    public string? Landusedemand { get; init; }
}

public sealed record LandDemandResponse
{
    public string Businessname { get; init; } = string.Empty;
    public string Creditcode { get; init; } = string.Empty;
    public string County { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public string? Area { get; init; }
    public decimal? BuildingArea { get; init; }
    public string? ExpectPark { get; init; }
    public string? ExpectTime { get; init; }
    public string? IsDeploy { get; init; }
    public string? DeployPark { get; init; }
    public string? IsSpecialuse { get; init; }
    public string? DeployLandtype { get; init; }
    public decimal? DeployHeight { get; init; }
    public decimal? DeployWeight { get; init; }
    public decimal? Investment { get; init; }
    public string? ProjectHydm { get; init; }
    public string? Keyindustry { get; init; }
    public string? Futureindustry { get; init; }
    public decimal? PredYs { get; init; }
    public decimal? PredTax { get; init; }
    public decimal? PredRdex { get; init; }
    public decimal? PredUnitenergy { get; init; }
    public string? Projectdata { get; init; }
    public string? IsFinancing { get; init; }
    public decimal? FinancingMoney { get; init; }
    public string? FinancingTime { get; init; }
    public string? Contact { get; init; }
    public string? Office { get; init; }
    public string? Phone { get; init; }
    public string? Landusedemand { get; init; }
    public string? Updatetime { get; init; }
}

public enum LandDemandOperationStatus
{
    Success,
    EnterpriseNotFound,
    NotFound,
    Exists,
    InvalidRequest
}

public sealed record LandDemandOperationResult(
    LandDemandOperationStatus Status,
    LandDemandResponse? Record);

public sealed record LandDemandValidationError(string Field, string Message);
