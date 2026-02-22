using Microsoft.EntityFrameworkCore;
using SaaS.ProjectManagement.Application.Abstractions.Persistence;
using SaaS.ProjectManagement.Application.Abstractions.Security;
using SaaS.ProjectManagement.Application.Contracts.Dashboard;
using SaaS.ProjectManagement.Domain.Enums;

namespace SaaS.ProjectManagement.Application.Services;

public sealed class DashboardService(IAppDbContext dbContext, ICurrentUserContext currentUser)
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var orgId = currentUser.OrganizationId;
        var now = DateTime.UtcNow;

        var totalProjects = await dbContext.Projects.CountAsync(p => p.OrganizationId == orgId, cancellationToken);
        var totalTasks = await dbContext.WorkItems.CountAsync(w => w.OrganizationId == orgId, cancellationToken);
        var completedTasks = await dbContext.WorkItems.CountAsync(w => w.OrganizationId == orgId && w.Status == WorkItemStatus.Done, cancellationToken);
        var inProgressTasks = await dbContext.WorkItems.CountAsync(w => w.OrganizationId == orgId && w.Status == WorkItemStatus.InProgress, cancellationToken);
        var overdueTasks = await dbContext.WorkItems.CountAsync(w => w.OrganizationId == orgId && w.DueDateUtc < now && w.Status != WorkItemStatus.Done, cancellationToken);

        return new DashboardSummaryResponse(totalProjects, totalTasks, completedTasks, inProgressTasks, overdueTasks);
    }
}
