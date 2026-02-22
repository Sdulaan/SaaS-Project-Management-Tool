using SaaS.ProjectManagement.Domain.Common;
using SaaS.ProjectManagement.Domain.Enums;

namespace SaaS.ProjectManagement.Domain.Entities;

public sealed class WorkItem : AuditableEntity
{
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? AssigneeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkItemStatus Status { get; set; } = WorkItemStatus.Backlog;
    public WorkItemPriority Priority { get; set; } = WorkItemPriority.Medium;
    public DateTime? DueDateUtc { get; set; }
    public int StoryPoints { get; set; }

    public Project Project { get; set; } = null!;
    public ApplicationUser? Assignee { get; set; }
    public ICollection<WorkItemComment> Comments { get; set; } = new List<WorkItemComment>();
}
