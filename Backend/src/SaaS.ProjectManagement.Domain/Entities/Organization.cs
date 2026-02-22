using SaaS.ProjectManagement.Domain.Common;

namespace SaaS.ProjectManagement.Domain.Entities;

public sealed class Organization : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
