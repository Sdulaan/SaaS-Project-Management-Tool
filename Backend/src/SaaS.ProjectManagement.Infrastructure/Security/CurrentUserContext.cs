using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SaaS.ProjectManagement.Application.Abstractions.Security;

namespace SaaS.ProjectManagement.Infrastructure.Security;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    public Guid UserId => GetGuid(ClaimTypes.NameIdentifier, ClaimTypes.Name, "sub");
    public Guid OrganizationId => GetGuid("org_id");
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    private Guid GetGuid(params string[] claimTypes)
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return Guid.Empty;
        }

        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirstValue(claimType);
            if (Guid.TryParse(value, out var guid))
            {
                return guid;
            }
        }

        return Guid.Empty;
    }
}
