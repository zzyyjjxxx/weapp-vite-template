using Microsoft.AspNetCore.Http;

namespace ForguncyServerApi.Api;

internal static class ApiErrorDiagnostics
{
    private const string DiagnosticsQueryParameter = "diagnostics";

    private static readonly IReadOnlyDictionary<string, string> SafeMessages =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["The Forguncy authentication connection configuration is invalid."] =
                "forguncy_connection_config_invalid",
            ["The Forguncy SMS configuration is invalid."] = "sms_config_invalid",
            ["The SMS verification storage is invalid."] = "sms_verification_storage_invalid",
            ["The SMS message log storage is invalid."] = "sms_message_log_storage_invalid",
            ["SMS verification is not configured."] = "sms_not_configured",
            ["SMS authentication failed."] = "sms_authentication_failed",
            ["FGC_JWT_SIGNING_KEY is required."] = "jwt_signing_key_missing",
            ["FGC_JWT_SIGNING_KEY must be at least 32 characters."] = "jwt_signing_key_invalid",
            ["FGC_JWT_EXPIRES_MINUTES must be a positive integer."] = "jwt_lifetime_invalid",
            ["FGC_JWT_EXPIRES_MINUTES is out of range."] = "jwt_lifetime_out_of_range",
            ["FGC_JWT_REFRESH_EXPIRES_MINUTES must be a positive integer."] =
                "jwt_refresh_lifetime_invalid",
            ["FGC_JWT_REFRESH_EXPIRES_MINUTES is out of range."] =
                "jwt_refresh_lifetime_out_of_range",
            ["The generated verification code is invalid."] = "verification_code_generation_invalid",
            ["The send code result status is not supported."] = "send_code_result_invalid",
            ["The verify code result status is not supported."] = "verify_code_result_invalid",
            ["The login result status is not supported."] = "login_result_invalid",
            ["The refresh result status is not supported."] = "refresh_result_invalid",
            ["The land demand operation result is not supported."] = "land_demand_result_invalid"
        };

    public static object CreateServerError(
        HttpRequest request,
        string operationCode,
        Exception exception)
    {
        if (!IsEnabled(request))
        {
            return new Dictionary<string, string>
            {
                ["error"] = "server_error"
            };
        }

        var payload = new Dictionary<string, object>
        {
            ["error"] = "server_error",
            ["operation"] = operationCode,
            ["exception_type"] = exception.GetType().Name,
            ["detail_code"] = GetDetailCode(exception)
        };

        if (SafeMessages.ContainsKey(exception.Message))
        {
            payload["message"] = exception.Message;
        }

        return payload;
    }

    private static bool IsEnabled(HttpRequest request)
    {
        var value = request.Query[DiagnosticsQueryParameter].ToString();
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetDetailCode(Exception exception)
    {
        if (SafeMessages.TryGetValue(exception.Message, out var knownCode))
        {
            return knownCode;
        }

        return exception.GetType().Name switch
        {
            "SqlSugarException" or "MySqlException" or "DbException" => "database_error",
            "HttpRequestException" => "external_http_request_failed",
            "TaskCanceledException" or "TimeoutException" => "external_service_timeout",
            "JsonReaderException" or "JsonSerializationException" => "external_response_invalid",
            "ArgumentException" => "invalid_argument",
            _ => "unexpected_exception"
        };
    }
}
