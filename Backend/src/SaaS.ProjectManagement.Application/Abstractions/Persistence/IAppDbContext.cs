using Microsoft.EntityFrameworkCore;
using SaaS.ProjectManagement.Domain.Entities;

namespace SaaS.ProjectManagement.Application.Abstractions.Persistence;

public interface IAppDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<ApplicationUser> Users { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectMember> ProjectMembers { get; }
    DbSet<WorkItem> WorkItems { get; }
    DbSet<WorkItemComment> WorkItemComments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
