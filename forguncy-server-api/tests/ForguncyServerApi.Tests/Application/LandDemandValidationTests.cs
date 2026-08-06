using System.Reflection;
using ForguncyServerApi.Application;
using Xunit;

namespace ForguncyServerApi.Tests.Application;

public sealed class LandDemandValidationTests
{
    [Fact]
    public void LandDemand_write_request_contract_exposes_only_the_writable_fields()
    {
        var propertyNames = typeof(LandDemandWriteRequest)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "Area",
                "BuildingArea",
                "Contact",
                "DeployHeight",
                "DeployLandtype",
                "DeployPark",
                "DeployWeight",
                "ExpectPark",
                "ExpectTime",
                "FinancingMoney",
                "FinancingTime",
                "Futureindustry",
                "Investment",
                "IsDeploy",
                "IsFinancing",
                "IsSpecialuse",
                "Keyindustry",
                "Landusedemand",
                "Office",
                "Phone",
                "PredRdex",
                "PredTax",
                "PredUnitenergy",
                "PredYs",
                "ProjectHydm",
                "Projectdata"
            },
            propertyNames);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    public void Validate_accepts_only_draft_or_submitted_status(string status)
    {
        Assert.True(LandDemandValidation.IsSupportedStatus(status));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("3")]
    [InlineData("draft")]
    public void Validate_rejects_unsupported_status(string? status)
    {
        var request = ValidSubmittedRequest() with
        {
            Landusedemand = status
        };

        var errors = LandDemandValidation.Validate(request);

        Assert.Contains(errors, error => error.Field == "landusedemand");
    }

    [Fact]
    public void Validate_draft_permits_missing_fields_but_checks_supplied_values()
    {
        var request = new LandDemandWriteRequest
        {
            Landusedemand = "2",
            FinancingMoney = -1m,
            ExpectTime = "2026-13"
        };

        var errors = LandDemandValidation.Validate(request);

        Assert.DoesNotContain(errors, error => error.Field == "area");
        Assert.Contains(errors, error => error.Field == "financing_money");
        Assert.Contains(errors, error => error.Field == "expect_time");
    }

    [Fact]
    public void Validate_submission_requires_the_approved_required_fields()
    {
        var request = new LandDemandWriteRequest
        {
            Landusedemand = "1"
        };

        var errors = LandDemandValidation.Validate(request);
        var fields = errors.Select(error => error.Field).ToArray();

        Assert.Contains("area", fields);
        Assert.Contains("building_area", fields);
        Assert.Contains("expect_park", fields);
        Assert.Contains("expect_time", fields);
        Assert.Contains("is_deploy", fields);
        Assert.Contains("is_specialuse", fields);
        Assert.Contains("investment", fields);
        Assert.Contains("project_hydm", fields);
        Assert.Contains("keyindustry", fields);
        Assert.Contains("futureindustry", fields);
        Assert.Contains("pred_ys", fields);
        Assert.Contains("pred_tax", fields);
        Assert.Contains("pred_rdex", fields);
        Assert.Contains("pred_unitenergy", fields);
        Assert.Contains("projectdata", fields);
        Assert.Contains("is_financing", fields);
        Assert.Contains("contact", fields);
        Assert.Contains("phone", fields);
    }

    [Fact]
    public void Validate_submission_requires_financing_fields_when_financing_is_one()
    {
        var request = ValidSubmittedRequest() with
        {
            IsFinancing = "1",
            FinancingMoney = null,
            FinancingTime = null
        };

        var errors = LandDemandValidation.Validate(request);

        Assert.Contains("financing_money", errors.Select(error => error.Field));
        Assert.Contains("financing_time", errors.Select(error => error.Field));
    }

    [Fact]
    public void Validate_submission_requires_deploy_park_when_deploy_is_one()
    {
        var request = ValidSubmittedRequest() with
        {
            IsDeploy = "1",
            DeployPark = null
        };

        var errors = LandDemandValidation.Validate(request);

        Assert.Contains(errors, error => error.Field == "deploy_park");
    }

    [Fact]
    public void Validate_submission_requires_deploy_landtype_when_specialuse_is_one()
    {
        var request = ValidSubmittedRequest() with
        {
            IsSpecialuse = "1",
            DeployLandtype = null
        };

        var errors = LandDemandValidation.Validate(request);

        Assert.Contains(errors, error => error.Field == "deploy_landtype");
    }

    [Fact]
    public void Validate_rejects_negative_and_out_of_range_decimal_values()
    {
        var request = ValidSubmittedRequest() with
        {
            BuildingArea = -1m,
            FinancingMoney = 100000000m,
            PredYs = 100000000000000m
        };

        var errors = LandDemandValidation.Validate(request);

        Assert.Contains(errors, error => error.Field == "building_area");
        Assert.Contains(errors, error => error.Field == "financing_money");
        Assert.Contains(errors, error => error.Field == "pred_ys");
    }

    [Fact]
    public void Validate_accepts_boundary_decimal_values_and_month_strings()
    {
        var request = ValidSubmittedRequest() with
        {
            BuildingArea = 99999999.99m,
            DeployHeight = 99999999.99m,
            DeployWeight = 99999999.99m,
            FinancingMoney = 99999999.99m,
            Investment = 99999999999999.99m,
            PredTax = 99999999999999.99m,
            PredRdex = 99999999999999.99m,
            PredYs = 99999999999999.99m,
            PredUnitenergy = 99999999999999.99m,
            ExpectTime = "2026-08",
            FinancingTime = "2027-12"
        };

        var errors = LandDemandValidation.Validate(request);

        Assert.Empty(errors);
    }

    private static LandDemandWriteRequest ValidSubmittedRequest() =>
        new()
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
}
