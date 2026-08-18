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
            var json = await ReadBodyAsync(request.Body, cancellationToken);
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
        catch (DecoderFallbackException)
        {
            throw new LandDemandRequestFormatException();
        }
    }

    private static async Task<string> ReadBodyAsync(
        Stream body,
        CancellationToken cancellationToken)
    {
        using var content = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bytesRead = await body.ReadAsync(
                buffer,
                0,
                buffer.Length,
                cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            content.Write(buffer, 0, bytesRead);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var json = new UTF8Encoding(false, true).GetString(content.ToArray());
        return json.Length > 0 && json[0] == '\uFEFF'
            ? json.Substring(1)
            : json;
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
