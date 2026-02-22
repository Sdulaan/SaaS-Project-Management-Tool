namespace SaaS.ProjectManagement.Application.Contracts.Dashboard;

public sealed record DashboardSummaryResponse(int TotalProjects, int TotalTasks, int CompletedTasks, int InProgressTasks, int OverdueTasks);
