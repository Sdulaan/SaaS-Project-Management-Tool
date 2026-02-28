using Microsoft.EntityFrameworkCore;
using SaaS.ProjectManagement.Application.Abstractions.Persistence;
using SaaS.ProjectManagement.Application.Abstractions.Security;
using SaaS.ProjectManagement.Application.Common.Exceptions;
using SaaS.ProjectManagement.Application.Contracts.Projects;
using SaaS.ProjectManagement.Domain.Entities;
using SaaS.ProjectManagement.Domain.Enums;

namespace SaaS.ProjectManagement.Application.Services;

public sealed class ProjectService(IAppDbContext dbContext, ICurrentUserContext currentUser)
{
    public async Task<IReadOnlyList<ProjectResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var orgId = currentUser.OrganizationId;

        return await dbContext.Projects
            .Where(p => p.OrganizationId == orgId && !p.IsCompleted)
            .OrderBy(p => p.Name)
            .Select(p => new ProjectResponse(
                p.Id,
                p.Name,
                p.Description,
                p.DueDateUtc,
                p.WorkItems.Count,
                p.WorkItems.Count(w => w.Status == WorkItemStatus.Done),
                p.IsCompleted))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new AppException("Project name is required.");
        }

        var project = new Project
        {
            OrganizationId = currentUser.OrganizationId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            DueDateUtc = request.DueDateUtc
        };

        dbContext.Projects.Add(project);
        dbContext.ProjectMembers.Add(new ProjectMember
        {
            Project = project,
            UserId = currentUser.UserId,
            Role = ProjectRole.ProjectManager
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ProjectResponse(project.Id, project.Name, project.Description, project.DueDateUtc, 0, 0, false);
    }

    public async Task DeleteAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FirstOrDefaultAsync(
            p => p.Id == projectId && p.OrganizationId == currentUser.OrganizationId,
            cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        dbContext.Projects.Remove(project);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectResponse> CompleteProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .Include(p => p.WorkItems)
            .FirstOrDefaultAsync(
                p => p.Id == projectId && p.OrganizationId == currentUser.OrganizationId,
                cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        var incompleteStatuses = project.WorkItems
            .Where(w => w.Status != WorkItemStatus.Done)
            .Select(w => w.Status)
            .Distinct()
            .ToList();

        if (incompleteStatuses.Any())
        {
            var statusNames = incompleteStatuses
                .Select(s => Enum.GetName(typeof(WorkItemStatus), s))
                .ToList();
            throw new AppException($"Cannot complete project. Not all tasks are finished. Incomplete statuses: {string.Join(", ", statusNames)}");
        }

        project.IsCompleted = true;
        project.UpdatedUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ProjectResponse(
            project.Id,
            project.Name,
            project.Description,
            project.DueDateUtc,
            project.WorkItems.Count,
            project.WorkItems.Count(w => w.Status == WorkItemStatus.Done),
            project.IsCompleted);
    }

    public async Task<IReadOnlyList<CompletedProjectResponse>> GetCompletedAsync(CancellationToken cancellationToken)
    {
        var orgId = currentUser.OrganizationId;

        return await dbContext.Projects
            .Where(p => p.OrganizationId == orgId && p.IsCompleted)
            .OrderByDescending(p => p.UpdatedUtc)
            .Select(p => new CompletedProjectResponse(
                p.Id,
                p.Name,
                p.Description,
                p.DueDateUtc,
                p.WorkItems.Count,
                p.WorkItems.Count(w => w.Status == WorkItemStatus.Done),
                p.WorkItems.Where(w => w.AssigneeId.HasValue).Select(w => w.AssigneeId).Distinct().Count(),
                p.UpdatedUtc!.Value))
            .ToListAsync(cancellationToken);
    }
}
