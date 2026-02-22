namespace SaaS.ProjectManagement.Application.Contracts.Auth;

public sealed record RegisterRequest(string OrganizationName, string FullName, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string Token, Guid UserId, Guid OrganizationId, string Email, string FullName, string Role);
