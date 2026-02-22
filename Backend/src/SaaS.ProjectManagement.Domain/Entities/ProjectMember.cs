using SaaS.ProjectManagement.Domain.Common;
using SaaS.ProjectManagement.Domain.Enums;

namespace SaaS.ProjectManagement.Domain.Entities;

public sealed class ProjectMember : AuditableEntity
{
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public ProjectRole Role { get; set; } = ProjectRole.Contributor;

    public Project Project { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
