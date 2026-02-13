using Microsoft.EntityFrameworkCore;

namespace TodoApp.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for the TodoApp application.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets will be added here as domain entities are created
    // Example:
    // public DbSet<User> Users { get; set; } = null!;
    // public DbSet<TodoTask> Tasks { get; set; } = null!;
    // public DbSet<Project> Projects { get; set; } = null!;
    // public DbSet<Label> Labels { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations using Fluent API
        // This will be populated with entity configuration classes as entities are created
        // Example:
        // modelBuilder.ApplyConfiguration(new UserConfiguration());
        // modelBuilder.ApplyConfiguration(new TodoTaskConfiguration());
        // modelBuilder.ApplyConfiguration(new ProjectConfiguration());
        // modelBuilder.ApplyConfiguration(new LabelConfiguration());
    }
}
