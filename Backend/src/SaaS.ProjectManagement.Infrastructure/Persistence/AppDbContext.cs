using Microsoft.EntityFrameworkCore;
using SaaS.ProjectManagement.Application.Abstractions.Persistence;
using SaaS.ProjectManagement.Domain.Entities;

namespace SaaS.ProjectManagement.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<WorkItemComment> WorkItemComments => Set<WorkItemComment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Slug).HasMaxLength(120);
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.FullName).HasMaxLength(200);
            entity.HasOne(x => x.Organization)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(180);
            entity.HasOne(x => x.Organization)
                .WithMany(x => x.Projects)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.HasIndex(x => new { x.ProjectId, x.UserId }).IsUnique();
            entity.HasOne(x => x.Project)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                .WithMany(x => x.ProjectMemberships)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkItem>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.HasOne(x => x.Project)
                .WithMany(x => x.WorkItems)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Assignee)
                .WithMany()
                .HasForeignKey(x => x.AssigneeId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<WorkItemComment>(entity =>
        {
            entity.Property(x => x.Body).HasMaxLength(3000);
            entity.HasOne(x => x.WorkItem)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.WorkItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(modelBuilder);
    }
}
