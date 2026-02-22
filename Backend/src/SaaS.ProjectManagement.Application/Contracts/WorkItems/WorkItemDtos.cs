using SaaS.ProjectManagement.Domain.Enums;

namespace SaaS.ProjectManagement.Application.Contracts.WorkItems;

public sealed record CreateWorkItemRequest(Guid ProjectId, string Title, string? Description, Guid? AssigneeId, WorkItemPriority Priority, DateTime? DueDateUtc, int StoryPoints);
public sealed record UpdateWorkItemStatusRequest(WorkItemStatus Status);
public sealed record WorkItemResponse(Guid Id, Guid ProjectId, string Title, string? Description, WorkItemStatus Status, WorkItemPriority Priority, Guid? AssigneeId, DateTime? DueDateUtc, int StoryPoints);
