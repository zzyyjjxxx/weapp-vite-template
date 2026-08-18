using Newtonsoft.Json;

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
    public string? Contact { get; init; }
    public string? Office { get; init; }
    public string? Phone { get; init; }
    public string? Landusedemand { get; init; }

    public LandDemandWriteRequest Normalize() =>
        this with
        {
            Area = Normalize(Area),
            ExpectPark = Normalize(ExpectPark),
            ExpectTime = Normalize(ExpectTime),
            IsDeploy = Normalize(IsDeploy),
            DeployPark = Normalize(DeployPark),
            IsSpecialuse = Normalize(IsSpecialuse),
            DeployLandtype = Normalize(DeployLandtype),
            ProjectHydm = Normalize(ProjectHydm),
            Keyindustry = Normalize(Keyindustry),
            Futureindustry = Normalize(Futureindustry),
            Projectdata = Normalize(Projectdata),
            Contact = Normalize(Contact),
            Office = Normalize(Office),
            Phone = Normalize(Phone),
            Landusedemand = Normalize(Landusedemand)
        };

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
}

public sealed record LandDemandResponse
{
    [JsonProperty("businessname")]
    public string Businessname { get; init; } = string.Empty;

    [JsonProperty("creditcode")]
    public string Creditcode { get; init; } = string.Empty;

    [JsonProperty("county")]
    public string County { get; init; } = string.Empty;

    [JsonProperty("region")]
    public string Region { get; init; } = string.Empty;

    [JsonProperty("area")]
    public string? Area { get; init; }

    [JsonProperty("building_area")]
    public decimal? BuildingArea { get; init; }

    [JsonProperty("expect_park")]
    public string? ExpectPark { get; init; }

    [JsonProperty("expect_time")]
    public string? ExpectTime { get; init; }

    [JsonProperty("is_deploy")]
    public string? IsDeploy { get; init; }

    [JsonProperty("deploy_park")]
    public string? DeployPark { get; init; }

    [JsonProperty("is_specialuse")]
    public string? IsSpecialuse { get; init; }

    [JsonProperty("deploy_landtype")]
    public string? DeployLandtype { get; init; }

    [JsonProperty("deploy_height")]
    public decimal? DeployHeight { get; init; }

    [JsonProperty("deploy_weight")]
    public decimal? DeployWeight { get; init; }

    [JsonProperty("investment")]
    public decimal? Investment { get; init; }

    [JsonProperty("project_hydm")]
    public string? ProjectHydm { get; init; }

    [JsonProperty("keyindustry")]
    public string? Keyindustry { get; init; }

    [JsonProperty("futureindustry")]
    public string? Futureindustry { get; init; }

    [JsonProperty("pred_ys")]
    public decimal? PredYs { get; init; }

    [JsonProperty("pred_tax")]
    public decimal? PredTax { get; init; }

    [JsonProperty("pred_rdex")]
    public decimal? PredRdex { get; init; }

    [JsonProperty("pred_unitenergy")]
    public decimal? PredUnitenergy { get; init; }

    [JsonProperty("projectdata")]
    public string? Projectdata { get; init; }

    [JsonProperty("contact")]
    public string? Contact { get; init; }

    [JsonProperty("office")]
    public string? Office { get; init; }

    [JsonProperty("phone")]
    public string? Phone { get; init; }

    [JsonProperty("landusedemand")]
    public string? Landusedemand { get; init; }

    [JsonProperty("updatetime")]
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
