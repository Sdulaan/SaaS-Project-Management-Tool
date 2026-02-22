using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaS.ProjectManagement.Application.Contracts.Dashboard;
using SaaS.ProjectManagement.Application.Services;

namespace SaaS.ProjectManagement.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(DashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(CancellationToken cancellationToken)
        => Ok(await dashboardService.GetSummaryAsync(cancellationToken));
}
