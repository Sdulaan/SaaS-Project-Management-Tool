using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaS.ProjectManagement.Application.Contracts.Projects;
using SaaS.ProjectManagement.Application.Services;

namespace SaaS.ProjectManagement.API.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public sealed class ProjectsController(ProjectService projectService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectResponse>>> Get(CancellationToken cancellationToken)
        => Ok(await projectService.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(CreateProjectRequest request, CancellationToken cancellationToken)
        => Ok(await projectService.CreateAsync(request, cancellationToken));

    [HttpDelete("{projectId:guid}")]
    public async Task<IActionResult> Delete(Guid projectId, CancellationToken cancellationToken)
    {
        await projectService.DeleteAsync(projectId, cancellationToken);
        return NoContent();
    }
}
