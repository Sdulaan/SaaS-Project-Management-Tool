namespace SaaS.ProjectManagement.Application.Abstractions.Security;

public interface IJwtTokenGenerator
{
    string Generate(Guid userId, Guid organizationId, string email, string role);
}
