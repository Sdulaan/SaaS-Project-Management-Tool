using Microsoft.EntityFrameworkCore;
using SaaS.ProjectManagement.Application.Abstractions.Persistence;
using SaaS.ProjectManagement.Application.Abstractions.Security;
using SaaS.ProjectManagement.Application.Common.Exceptions;
using SaaS.ProjectManagement.Application.Contracts.Members;
using SaaS.ProjectManagement.Application.Contracts.WorkItems;
using SaaS.ProjectManagement.Domain.Entities;
using SaaS.ProjectManagement.Domain.Enums;

namespace SaaS.ProjectManagement.Application.Services;

public sealed class MembersService(IAppDbContext dbContext, ICurrentUserContext currentUser, IPasswordHasher passwordHasher)
{
    public async Task<IReadOnlyList<MemberResponse>> GetOrganizationMembersAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .Where(u => u.OrganizationId == currentUser.OrganizationId)
            .OrderBy(u => u.FullName)
            .Select(u => new MemberResponse(u.Id, u.FullName, u.Email, u.Role))
            .ToListAsync(cancellationToken);
    }

    public async Task<MemberResponse> AddMemberAsync(AddMemberRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new AppException("Email is required.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Check if email already exists in organization
        var exists = await dbContext.Users.AnyAsync(
            u => u.Email == normalizedEmail && u.OrganizationId == currentUser.OrganizationId,
            cancellationToken);

        if (exists)
        {
            throw new AppException("Email is already a member of this organization.");
        }

        // Generate temporary password
        var temporaryPassword = GenerateTemporaryPassword();

        var user = new ApplicationUser
        {
            OrganizationId = currentUser.OrganizationId,
            FullName = normalizedEmail.Split('@')[0], // Use email prefix as default name
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(temporaryPassword),
            Role = UserRole.Member
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MemberResponse(user.Id, user.FullName, user.Email, user.Role);
    }

    public async Task RemoveMemberAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Prevent removing self
        if (userId == currentUser.UserId)
        {
            throw new AppException("You cannot remove yourself from the organization.");
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(
            u => u.Id == userId && u.OrganizationId == currentUser.OrganizationId,
            cancellationToken)
            ?? throw new NotFoundException("Member not found.");

        // Prevent removing organization owner
        if (user.Role == UserRole.Owner)
        {
            throw new AppException("Cannot remove the organization owner.");
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<MemberResponse> UpdateAssigneeAsync(Guid workItemId, Guid? assigneeId, CancellationToken cancellationToken)
    {
        var workItem = await dbContext.WorkItems.FirstOrDefaultAsync(
            w => w.Id == workItemId && w.OrganizationId == currentUser.OrganizationId,
            cancellationToken)
            ?? throw new NotFoundException("Task not found.");

        // Validate assignee exists in organization if provided
        if (assigneeId.HasValue)
        {
            var assigneeExists = await dbContext.Users.AnyAsync(
                u => u.Id == assigneeId.Value && u.OrganizationId == currentUser.OrganizationId,
                cancellationToken);

            if (!assigneeExists)
            {
                throw new NotFoundException("Member not found.");
            }
        }

        workItem.AssigneeId = assigneeId;
        workItem.UpdatedUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        // Return updated work item with assignee info
        var assignee = workItem.AssigneeId.HasValue
            ? await dbContext.Users.FirstOrDefaultAsync(u => u.Id == workItem.AssigneeId, cancellationToken)
            : null;

        return new WorkItemResponse(
            workItem.Id,
            workItem.ProjectId,
            workItem.Title,
            workItem.Description,
            workItem.Status,
            workItem.Priority,
            workItem.AssigneeId,
            assignee?.FullName,
            assignee?.Email,
            workItem.DueDateUtc,
            workItem.StoryPoints);
    }

    private static string GenerateTemporaryPassword()
    {
        // Generate a 12-character temporary password
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Range(0, 12)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }
}
