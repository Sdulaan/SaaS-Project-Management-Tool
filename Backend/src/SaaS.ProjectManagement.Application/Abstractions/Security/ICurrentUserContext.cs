namespace SaaS.ProjectManagement.Application.Abstractions.Security;

public interface ICurrentUserContext
{
    Guid UserId { get; }
    Guid OrganizationId { get; }
    bool IsAuthenticated { get; }
}
