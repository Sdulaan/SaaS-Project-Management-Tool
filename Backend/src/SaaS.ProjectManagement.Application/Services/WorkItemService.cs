using Microsoft.EntityFrameworkCore;
using SaaS.ProjectManagement.Application.Abstractions.Persistence;
using SaaS.ProjectManagement.Application.Abstractions.Security;
using SaaS.ProjectManagement.Application.Common.Exceptions;
using SaaS.ProjectManagement.Application.Contracts.WorkItems;
using SaaS.ProjectManagement.Domain.Entities;

namespace SaaS.ProjectManagement.Application.Services;

public sealed class WorkItemService(IAppDbContext dbContext, ICurrentUserContext currentUser)
{
    public async Task<IReadOnlyList<WorkItemResponse>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await dbContext.WorkItems
            .Where(w => w.ProjectId == projectId && w.OrganizationId == currentUser.OrganizationId)
            .OrderBy(w => w.Status)
            .ThenBy(w => w.Priority)
            .Select(w => new WorkItemResponse(w.Id, w.ProjectId, w.Title, w.Description, w.Status, w.Priority, w.AssigneeId, w.DueDateUtc, w.StoryPoints))
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkItemResponse> CreateAsync(CreateWorkItemRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new AppException("Task title is required.");
        }

        var projectExists = await dbContext.Projects.AnyAsync(
            p => p.Id == request.ProjectId && p.OrganizationId == currentUser.OrganizationId,
            cancellationToken);

        if (!projectExists)
        {
            throw new NotFoundException("Project not found.");
        }

        var workItem = new WorkItem
        {
            OrganizationId = currentUser.OrganizationId,
            ProjectId = request.ProjectId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            AssigneeId = request.AssigneeId,
            Priority = request.Priority,
            DueDateUtc = request.DueDateUtc,
            StoryPoints = request.StoryPoints
        };

        dbContext.WorkItems.Add(workItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new WorkItemResponse(workItem.Id, workItem.ProjectId, workItem.Title, workItem.Description, workItem.Status, workItem.Priority, workItem.AssigneeId, workItem.DueDateUtc, workItem.StoryPoints);
    }

    public async Task<WorkItemResponse> UpdateStatusAsync(Guid workItemId, UpdateWorkItemStatusRequest request, CancellationToken cancellationToken)
    {
        var workItem = await dbContext.WorkItems.FirstOrDefaultAsync(
            w => w.Id == workItemId && w.OrganizationId == currentUser.OrganizationId,
            cancellationToken)
            ?? throw new NotFoundException("Task not found.");

        workItem.Status = request.Status;
        workItem.UpdatedUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new WorkItemResponse(workItem.Id, workItem.ProjectId, workItem.Title, workItem.Description, workItem.Status, workItem.Priority, workItem.AssigneeId, workItem.DueDateUtc, workItem.StoryPoints);
    }
}
