using System.Globalization;
using System.Text.RegularExpressions;

namespace ForguncyServerApi.Application;

public static class LandDemandValidation
{
    private static readonly Regex YearMonthPattern = new("^(19|20)\\d{2}-(0[1-9]|1[0-2])$", RegexOptions.Compiled);

    public static bool IsSupportedStatus(string? status) =>
        string.Equals(status, "1", StringComparison.Ordinal)
        || string.Equals(status, "2", StringComparison.Ordinal);

    public static IReadOnlyList<LandDemandValidationError> Validate(LandDemandWriteRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return ValidateNormalized(request.Normalize());
    }

    internal static IReadOnlyList<LandDemandValidationError> ValidateNormalized(LandDemandWriteRequest request)
    {
        var errors = new List<LandDemandValidationError>();

        if (!IsSupportedStatus(request.Landusedemand))
        {
            errors.Add(new("landusedemand", "Only draft (2) and submitted (1) statuses are supported."));
            return errors;
        }

        var isSubmitted = string.Equals(request.Landusedemand, "1", StringComparison.Ordinal);

        if (isSubmitted)
        {
            RequireString(errors, "area", request.Area);
            RequireDecimal(errors, "building_area", request.BuildingArea);
            RequireString(errors, "expect_park", request.ExpectPark);
            RequireString(errors, "expect_time", request.ExpectTime);
            RequireString(errors, "is_deploy", request.IsDeploy);
            RequireString(errors, "is_specialuse", request.IsSpecialuse);
            RequireDecimal(errors, "investment", request.Investment);
            RequireString(errors, "project_hydm", request.ProjectHydm);
            RequireString(errors, "keyindustry", request.Keyindustry);
            RequireString(errors, "futureindustry", request.Futureindustry);
            RequireDecimal(errors, "pred_ys", request.PredYs);
            RequireDecimal(errors, "pred_tax", request.PredTax);
            RequireDecimal(errors, "pred_rdex", request.PredRdex);
            RequireDecimal(errors, "pred_unitenergy", request.PredUnitenergy);
            RequireString(errors, "projectdata", request.Projectdata);
            RequireString(errors, "is_financing", request.IsFinancing);
            RequireString(errors, "contact", request.Contact);
            RequireString(errors, "phone", request.Phone);
        }

        ValidateYearMonth(errors, "expect_time", request.ExpectTime);
        ValidateYearMonth(errors, "financing_time", request.FinancingTime);

        ValidateDecimal(errors, "building_area", request.BuildingArea, 8, 2);
        ValidateDecimal(errors, "deploy_height", request.DeployHeight, 8, 2);
        ValidateDecimal(errors, "deploy_weight", request.DeployWeight, 8, 2);
        ValidateDecimal(errors, "financing_money", request.FinancingMoney, 14, 6);
        ValidateDecimal(errors, "investment", request.Investment, 14, 6);
        ValidateDecimal(errors, "pred_tax", request.PredTax, 14, 6);
        ValidateDecimal(errors, "pred_rdex", request.PredRdex, 14, 6);
        ValidateDecimal(errors, "pred_ys", request.PredYs, 14, 6);
        ValidateDecimal(errors, "pred_unitenergy", request.PredUnitenergy, 14, 6);

        if (IsAffirmative(request.IsDeploy) && string.IsNullOrWhiteSpace(request.DeployPark))
        {
            errors.Add(new("deploy_park", "deploy_park is required when is_deploy is affirmative."));
        }

        if (IsAffirmative(request.IsSpecialuse) && string.IsNullOrWhiteSpace(request.DeployLandtype))
        {
            errors.Add(new("deploy_landtype", "deploy_landtype is required when is_specialuse is affirmative."));
        }

        if (IsFinancingAffirmative(request.IsFinancing))
        {
            if (!request.FinancingMoney.HasValue)
            {
                errors.Add(new("financing_money", "financing_money is required when is_financing is affirmative."));
            }

            if (string.IsNullOrWhiteSpace(request.FinancingTime))
            {
                errors.Add(new("financing_time", "financing_time is required when is_financing is affirmative."));
            }
        }

        return errors;
    }

    private static bool IsAffirmative(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return string.Equals(value, "1", StringComparison.Ordinal)
            || string.Equals(value, "是", StringComparison.Ordinal)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFinancingAffirmative(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return string.Equals(value, "1", StringComparison.Ordinal)
            || string.Equals(value, "有", StringComparison.Ordinal);
    }

    private static void RequireString(List<LandDemandValidationError> errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new(field, $"{field} is required."));
        }
    }

    private static void RequireDecimal(List<LandDemandValidationError> errors, string field, decimal? value)
    {
        if (!value.HasValue)
        {
            errors.Add(new(field, $"{field} is required."));
        }
    }

    private static void ValidateYearMonth(List<LandDemandValidationError> errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmedValue = value!.Trim();
        if (!YearMonthPattern.IsMatch(trimmedValue))
        {
            errors.Add(new(field, $"{field} must use YYYY-MM format."));
        }
    }

    private static void ValidateDecimal(
        List<LandDemandValidationError> errors,
        string field,
        decimal? value,
        int maxIntegerDigits,
        int maxScale)
    {
        if (!value.HasValue)
        {
            return;
        }

        if (value.Value < 0)
        {
            errors.Add(new(field, $"{field} must be nonnegative."));
            return;
        }

        var normalized = Math.Abs(value.Value).ToString(CultureInfo.InvariantCulture);
        var parts = normalized.Split('.');
        var integerDigits = parts[0].TrimStart('0').Length;
        if (parts[0] == "0")
        {
            integerDigits = 0;
        }

        var scale = parts.Length > 1 ? parts[1].Length : 0;
        if (integerDigits > maxIntegerDigits || scale > maxScale)
        {
            errors.Add(new(field, $"{field} exceeds the supported precision."));
        }
    }
}
