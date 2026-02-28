using SaaS.ProjectManagement.Domain.Common;

namespace SaaS.ProjectManagement.Domain.Entities;

public sealed class Project : AuditableEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public bool IsCompleted { get; set; } = false;

    public Organization Organization { get; set; } = null!;
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
}
