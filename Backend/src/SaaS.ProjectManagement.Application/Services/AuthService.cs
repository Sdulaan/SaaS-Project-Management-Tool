using Microsoft.EntityFrameworkCore;
using SaaS.ProjectManagement.Application.Abstractions.Persistence;
using SaaS.ProjectManagement.Application.Abstractions.Security;
using SaaS.ProjectManagement.Application.Common.Exceptions;
using SaaS.ProjectManagement.Application.Contracts.Auth;
using SaaS.ProjectManagement.Domain.Entities;
using SaaS.ProjectManagement.Domain.Enums;

namespace SaaS.ProjectManagement.Application.Services;

public sealed class AuthService(IAppDbContext dbContext, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationName) || string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new AppException("All registration fields are required.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var exists = await dbContext.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (exists)
        {
            throw new AppException("Email is already registered.");
        }

        var organization = new Organization
        {
            Name = request.OrganizationName.Trim(),
            Slug = Slugify(request.OrganizationName)
        };

        var user = new ApplicationUser
        {
            Organization = organization,
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = UserRole.Owner
        };

        dbContext.Organizations.Add(organization);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var token = jwtTokenGenerator.Generate(user.Id, organization.Id, user.Email, user.Role.ToString());
        return new AuthResponse(token, user.Id, organization.Id, user.Email, user.FullName, user.Role.ToString());
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken)
            ?? throw new UnauthorizedAppException("Invalid credentials.");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAppException("Invalid credentials.");
        }

        var token = jwtTokenGenerator.Generate(user.Id, user.OrganizationId, user.Email, user.Role.ToString());
        return new AuthResponse(token, user.Id, user.OrganizationId, user.Email, user.FullName, user.Role.ToString());
    }

    private static string Slugify(string value)
    {
        var slug = value.Trim().ToLowerInvariant();
        slug = string.Join('-', slug.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return string.Concat(slug.Where(c => char.IsLetterOrDigit(c) || c == '-'));
    }
}
