namespace SaaS.ProjectManagement.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "SaaS.ProjectManagement";
    public string Audience { get; set; } = "SaaS.ProjectManagement.Client";
    public string Key { get; set; } = "super-secret-and-long-key-change-in-production";
    public int ExpiryMinutes { get; set; } = 120;
}
