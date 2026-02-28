namespace SaaS.ProjectManagement.Application.Contracts.Projects;

public sealed record CreateProjectRequest(string Name, string? Description, DateTime? DueDateUtc);
public sealed record ProjectResponse(Guid Id, string Name, string? Description, DateTime? DueDateUtc, int TotalTasks, int CompletedTasks, bool IsCompleted);
public sealed record CompleteProjectRequest();
public sealed record CompletedProjectResponse(Guid Id, string Name, string? Description, DateTime? DueDateUtc, int TotalTasks, int CompletedTasks, int MemberCount, DateTime CompletedAtUtc);
