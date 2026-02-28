using SaaS.ProjectManagement.Domain.Common;
using SaaS.ProjectManagement.Domain.Enums;

namespace SaaS.ProjectManagement.Domain.Entities;

public sealed class ApplicationUser : AuditableEntity
{
    public Guid OrganizationId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Member;

    public Organization Organization { get; set; } = null!;
    public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
}
