using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace ForguncyServerApi.Api;

public static class ApiResponseWriter
{
    public static async Task WriteJsonAsync(
        HttpResponse response,
        int statusCode,
        object value,
        CancellationToken cancellationToken)
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        response.StatusCode = statusCode;
        response.ContentType = "application/json; charset=utf-8";
        response.Headers["Cache-Control"] = "no-store";
        response.Headers["Pragma"] = "no-cache";
        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
        await response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
    }
}
