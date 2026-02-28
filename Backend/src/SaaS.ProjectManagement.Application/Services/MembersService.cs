using Microsoft.EntityFrameworkCore;
using SaaS.ProjectManagement.Application.Abstractions.Persistence;
using SaaS.ProjectManagement.Application.Abstractions.Security;
using SaaS.ProjectManagement.Application.Common.Exceptions;
using SaaS.ProjectManagement.Application.Contracts.Members;
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
            .Select(u => new MemberResponse(u.Id, u.FullName, u.DisplayName, u.Email, u.Role))
            .ToListAsync(cancellationToken);
    }

    public async Task<MemberResponse> AddMemberAsync(AddMemberRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new AppException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new AppException("Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new AppException("Display name is required.");
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
            FullName = request.FullName.Trim(),
            DisplayName = request.DisplayName.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(temporaryPassword),
            Role = UserRole.Member
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MemberResponse(user.Id, user.FullName, user.DisplayName, user.Email, user.Role);
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

