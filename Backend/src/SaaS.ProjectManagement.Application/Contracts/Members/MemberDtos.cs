using SaaS.ProjectManagement.Domain.Enums;

namespace SaaS.ProjectManagement.Application.Contracts.Members;

public sealed record MemberResponse(Guid Id, string FullName, string Email, UserRole Role);
public sealed record AddMemberRequest(string Email);
