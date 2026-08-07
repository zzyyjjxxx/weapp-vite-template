using System.Text;
using ForguncyServerApi.Api;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ForguncyServerApi.Tests.Api;

public sealed class LandDemandRequestReaderTests
{
    [Fact]
    public async Task ReadAsync_reads_every_writable_property_from_json()
    {
        var request = Request(
            "{" +
            "\"area\":\"330212\"," +
            "\"building_area\":1234.56," +
            "\"expect_park\":\"Synthetic Park\"," +
            "\"expect_time\":\"2026-12\"," +
            "\"is_deploy\":\"1\"," +
            "\"deploy_park\":\"Park A,Park B\"," +
            "\"is_specialuse\":\"1\"," +
            "\"deploy_landtype\":\"Industrial\"," +
            "\"deploy_height\":12.34," +
            "\"deploy_weight\":56.78," +
            "\"investment\":12345678901234.123456," +
            "\"project_hydm\":\"C3990\"," +
            "\"keyindustry\":\"Synthetic Track\"," +
            "\"futureindustry\":\"Synthetic Direction\"," +
            "\"pred_ys\":100.000001," +
            "\"pred_tax\":200.000002," +
            "\"pred_rdex\":300.000003," +
            "\"pred_unitenergy\":400.000004," +
            "\"projectdata\":\"Synthetic project\"," +
            "\"is_financing\":\"0\"," +
            "\"financing_money\":null," +
            "\"financing_time\":null," +
            "\"contact\":\"Synthetic Contact\"," +
            "\"office\":\"Synthetic Office\"," +
            "\"phone\":\"13800000000\"," +
            "\"landusedemand\":\"1\"}");

        var result = await LandDemandRequestReader.ReadAsync(request, CancellationToken.None);

        Assert.Equal("330212", result.Area);
        Assert.Equal(1234.56m, result.BuildingArea);
        Assert.Equal("Synthetic Park", result.ExpectPark);
        Assert.Equal("2026-12", result.ExpectTime);
        Assert.Equal("1", result.IsDeploy);
        Assert.Equal("Park A,Park B", result.DeployPark);
        Assert.Equal("1", result.IsSpecialuse);
        Assert.Equal("Industrial", result.DeployLandtype);
        Assert.Equal(12.34m, result.DeployHeight);
        Assert.Equal(56.78m, result.DeployWeight);
        Assert.Equal(12345678901234.123456m, result.Investment);
        Assert.Equal("C3990", result.ProjectHydm);
        Assert.Equal("Synthetic Track", result.Keyindustry);
        Assert.Equal("Synthetic Direction", result.Futureindustry);
        Assert.Equal(100.000001m, result.PredYs);
        Assert.Equal(200.000002m, result.PredTax);
        Assert.Equal(300.000003m, result.PredRdex);
        Assert.Equal(400.000004m, result.PredUnitenergy);
        Assert.Equal("Synthetic project", result.Projectdata);
        Assert.Equal("0", result.IsFinancing);
        Assert.Null(result.FinancingMoney);
        Assert.Null(result.FinancingTime);
        Assert.Equal("Synthetic Contact", result.Contact);
        Assert.Equal("Synthetic Office", result.Office);
        Assert.Equal("13800000000", result.Phone);
        Assert.Equal("1", result.Landusedemand);
    }

    [Fact]
    public async Task ReadAsync_allows_an_incomplete_draft_and_missing_optional_properties()
    {
        var result = await LandDemandRequestReader.ReadAsync(
            Request("{\"projectdata\":null,\"landusedemand\":\"2\"}"),
            CancellationToken.None);

        Assert.Equal("2", result.Landusedemand);
        Assert.Null(result.Area);
        Assert.Null(result.Projectdata);
        Assert.Null(result.BuildingArea);
    }

    [Theory]
    [InlineData("{ not-json }")]
    [InlineData("")]
    [InlineData("[]")]
    public async Task ReadAsync_rejects_malformed_or_non_object_json(string body)
    {
        await Assert.ThrowsAsync<LandDemandRequestFormatException>(
            () => LandDemandRequestReader.ReadAsync(Request(body), CancellationToken.None));
    }

    [Theory]
    [InlineData("{\"landusedemand\":1}")]
    [InlineData("{\"landusedemand\":true}")]
    [InlineData("{\"landusedemand\":null}")]
    [InlineData("{\"landusedemand\":\"3\"}")]
    [InlineData("{}")]
    public async Task ReadAsync_rejects_missing_non_string_or_unsupported_status(string body)
    {
        await Assert.ThrowsAsync<LandDemandRequestFormatException>(
            () => LandDemandRequestReader.ReadAsync(Request(body), CancellationToken.None));
    }

    [Theory]
    [InlineData("{\"area\":42,\"landusedemand\":\"2\"}")]
    [InlineData("{\"building_area\":\"12.34\",\"landusedemand\":\"2\"}")]
    [InlineData("{\"building_area\":true,\"landusedemand\":\"2\"}")]
    [InlineData("{\"building_area\":{},\"landusedemand\":\"2\"}")]
    public async Task ReadAsync_rejects_incorrect_property_types(string body)
    {
        await Assert.ThrowsAsync<LandDemandRequestFormatException>(
            () => LandDemandRequestReader.ReadAsync(Request(body), CancellationToken.None));
    }

    [Theory]
    [InlineData("creditcode")]
    [InlineData("businessname")]
    [InlineData("county")]
    [InlineData("region")]
    [InlineData("id")]
    [InlineData("updatetime")]
    [InlineData("updateuser")]
    [InlineData("region_remark")]
    [InlineData("county_isrecommend")]
    [InlineData("Area")]
    public async Task ReadAsync_rejects_identity_audit_internal_and_unknown_properties(string propertyName)
    {
        var body = $"{{\"{propertyName}\":\"injected\",\"landusedemand\":\"2\"}}";

        await Assert.ThrowsAsync<LandDemandRequestFormatException>(
            () => LandDemandRequestReader.ReadAsync(Request(body), CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsync_rejects_duplicate_properties()
    {
        await Assert.ThrowsAsync<LandDemandRequestFormatException>(
            () => LandDemandRequestReader.ReadAsync(
                Request("{\"landusedemand\":\"2\",\"landusedemand\":\"1\"}"),
                CancellationToken.None));
    }

    [Theory]
    [InlineData("application/x-www-form-urlencoded")]
    [InlineData("application/merge-patch+json")]
    [InlineData(null)]
    public async Task ReadAsync_accepts_only_application_json(string? contentType)
    {
        var request = Request("{\"landusedemand\":\"2\"}", contentType);

        await Assert.ThrowsAsync<LandDemandRequestFormatException>(
            () => LandDemandRequestReader.ReadAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsync_accepts_application_json_with_charset()
    {
        var result = await LandDemandRequestReader.ReadAsync(
            Request("{\"landusedemand\":\"2\"}", "application/json; charset=utf-8"),
            CancellationToken.None);

        Assert.Equal("2", result.Landusedemand);
    }

    private static HttpRequest Request(string body, string? contentType = "application/json")
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = contentType;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        return context.Request;
    }
}
