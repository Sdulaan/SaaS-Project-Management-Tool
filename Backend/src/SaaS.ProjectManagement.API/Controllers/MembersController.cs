using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaS.ProjectManagement.Application.Contracts.Members;
using SaaS.ProjectManagement.Application.Services;

namespace SaaS.ProjectManagement.API.Controllers;

[ApiController]
[Route("api/members")]
[Authorize]
public sealed class MembersController(MembersService membersService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MemberResponse>>> Get(CancellationToken cancellationToken)
        => Ok(await membersService.GetOrganizationMembersAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<MemberResponse>> Add(AddMemberRequest request, CancellationToken cancellationToken)
        => Ok(await membersService.AddMemberAsync(request, cancellationToken));

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Remove(Guid userId, CancellationToken cancellationToken)
    {
        await membersService.RemoveMemberAsync(userId, cancellationToken);
        return NoContent();
    }
}
