namespace ForguncyServerApi.Domain;

public sealed class EnterpriseProfile
{
    public int UserId { get; set; }

    public string CreditCode { get; set; } = string.Empty;

    public string BusinessName { get; set; } = string.Empty;

    public string CountyName { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;
}
