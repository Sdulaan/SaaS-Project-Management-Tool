using SaaS.ProjectManagement.Domain.Common;

namespace SaaS.ProjectManagement.Domain.Entities;

public sealed class WorkItemComment : AuditableEntity
{
    public Guid WorkItemId { get; set; }
    public Guid AuthorId { get; set; }
    public string Body { get; set; } = string.Empty;

    public WorkItem WorkItem { get; set; } = null!;
    public ApplicationUser Author { get; set; } = null!;
}
