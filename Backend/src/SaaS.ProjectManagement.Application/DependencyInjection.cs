using Microsoft.Extensions.DependencyInjection;
using SaaS.ProjectManagement.Application.Services;

namespace SaaS.ProjectManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<WorkItemService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<MembersService>();
        return services;
    }
}
