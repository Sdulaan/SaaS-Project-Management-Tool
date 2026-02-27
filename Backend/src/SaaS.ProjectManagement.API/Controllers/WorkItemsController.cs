using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaS.ProjectManagement.Application.Contracts.WorkItems;
using SaaS.ProjectManagement.Application.Services;

namespace SaaS.ProjectManagement.API.Controllers;

[ApiController]
[Route("api/work-items")]
[Authorize]
public sealed class WorkItemsController(WorkItemService workItemService) : ControllerBase
{
    [HttpGet("project/{projectId:guid}")]
    public async Task<ActionResult<IReadOnlyList<WorkItemResponse>>> GetByProject(Guid projectId, CancellationToken cancellationToken)
        => Ok(await workItemService.GetByProjectAsync(projectId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<WorkItemResponse>> Create(CreateWorkItemRequest request, CancellationToken cancellationToken)
        => Ok(await workItemService.CreateAsync(request, cancellationToken));

    [HttpPatch("{workItemId:guid}/status")]
    public async Task<ActionResult<WorkItemResponse>> UpdateStatus(Guid workItemId, UpdateWorkItemStatusRequest request, CancellationToken cancellationToken)
        => Ok(await workItemService.UpdateStatusAsync(workItemId, request, cancellationToken));

    [HttpPatch("{workItemId:guid}/assignee")]
    public async Task<ActionResult<WorkItemResponse>> UpdateAssignee(Guid workItemId, UpdateWorkItemAssigneeRequest request, CancellationToken cancellationToken)
        => Ok(await workItemService.UpdateAssigneeAsync(workItemId, request.AssigneeId, cancellationToken));
}
