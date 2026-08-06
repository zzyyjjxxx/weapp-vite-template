using System.Text;
using ForguncyServerApi.Application;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ForguncyServerApi.Api;

public sealed class LandDemandRequestFormatException : Exception
{
    public LandDemandRequestFormatException()
        : base("The land demand request format is invalid.")
    {
    }
}

public static class LandDemandRequestReader
{
    private static readonly HashSet<string> WritableProperties = new(StringComparer.Ordinal)
    {
        "area",
        "building_area",
        "expect_park",
        "expect_time",
        "is_deploy",
        "deploy_park",
        "is_specialuse",
        "deploy_landtype",
        "deploy_height",
        "deploy_weight",
        "investment",
        "project_hydm",
        "keyindustry",
        "futureindustry",
        "pred_ys",
        "pred_tax",
        "pred_rdex",
        "pred_unitenergy",
        "projectdata",
        "is_financing",
        "financing_money",
        "financing_time",
        "contact",
        "office",
        "phone",
        "landusedemand"
    };

    public static async Task<LandDemandWriteRequest> ReadAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (!IsApplicationJson(request.ContentType))
        {
            throw new LandDemandRequestFormatException();
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true);
            var json = await reader.ReadToEndAsync();
            cancellationToken.ThrowIfCancellationRequested();
            var payload = ParsePayload(json);

            RejectNonWritableProperties(payload);
            var status = ReadString(payload, "landusedemand");
            if (!LandDemandValidation.IsSupportedStatus(status))
            {
                throw new LandDemandRequestFormatException();
            }

            return new LandDemandWriteRequest
            {
                Area = ReadString(payload, "area"),
                BuildingArea = ReadDecimal(payload, "building_area"),
                ExpectPark = ReadString(payload, "expect_park"),
                ExpectTime = ReadString(payload, "expect_time"),
                IsDeploy = ReadString(payload, "is_deploy"),
                DeployPark = ReadString(payload, "deploy_park"),
                IsSpecialuse = ReadString(payload, "is_specialuse"),
                DeployLandtype = ReadString(payload, "deploy_landtype"),
                DeployHeight = ReadDecimal(payload, "deploy_height"),
                DeployWeight = ReadDecimal(payload, "deploy_weight"),
                Investment = ReadDecimal(payload, "investment"),
                ProjectHydm = ReadString(payload, "project_hydm"),
                Keyindustry = ReadString(payload, "keyindustry"),
                Futureindustry = ReadString(payload, "futureindustry"),
                PredYs = ReadDecimal(payload, "pred_ys"),
                PredTax = ReadDecimal(payload, "pred_tax"),
                PredRdex = ReadDecimal(payload, "pred_rdex"),
                PredUnitenergy = ReadDecimal(payload, "pred_unitenergy"),
                Projectdata = ReadString(payload, "projectdata"),
                IsFinancing = ReadString(payload, "is_financing"),
                FinancingMoney = ReadDecimal(payload, "financing_money"),
                FinancingTime = ReadString(payload, "financing_time"),
                Contact = ReadString(payload, "contact"),
                Office = ReadString(payload, "office"),
                Phone = ReadString(payload, "phone"),
                Landusedemand = status
            };
        }
        catch (JsonException)
        {
            throw new LandDemandRequestFormatException();
        }
        catch (FormatException)
        {
            throw new LandDemandRequestFormatException();
        }
        catch (OverflowException)
        {
            throw new LandDemandRequestFormatException();
        }
    }

    private static void RejectNonWritableProperties(JObject payload)
    {
        foreach (var property in payload.Properties())
        {
            if (!WritableProperties.Contains(property.Name))
            {
                throw new LandDemandRequestFormatException();
            }
        }
    }

    private static JObject ParsePayload(string json)
    {
        using var stringReader = new StringReader(json);
        using var jsonReader = new JsonTextReader(stringReader)
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Decimal
        };
        var payload = JObject.Load(
            jsonReader,
            new JsonLoadSettings
            {
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
            });
        if (jsonReader.Read())
        {
            throw new LandDemandRequestFormatException();
        }

        return payload;
    }

    private static string? ReadString(JObject payload, string name)
    {
        var token = payload[name];
        return token?.Type switch
        {
            null => null,
            JTokenType.Null => null,
            JTokenType.String => token.Value<string>(),
            _ => throw new LandDemandRequestFormatException()
        };
    }

    private static decimal? ReadDecimal(JObject payload, string name)
    {
        var token = payload[name];
        return token?.Type switch
        {
            null => null,
            JTokenType.Null => null,
            JTokenType.Integer => token.Value<decimal>(),
            JTokenType.Float => token.Value<decimal>(),
            _ => throw new LandDemandRequestFormatException()
        };
    }

    private static bool IsApplicationJson(string? contentType)
    {
        var mediaType = contentType?.Split(new[] { ';' }, 2)[0].Trim();
        return string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase);
    }
}
