using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Enums;
using TaskStatus = TodoApp.Domain.Enums.TaskStatus;

namespace TodoApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the TodoTask entity.
/// </summary>
public class TodoTaskConfiguration : IEntityTypeConfiguration<TodoTask>
{
    public void Configure(EntityTypeBuilder<TodoTask> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnType("uuid")
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasColumnType("uuid")
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnType("text")
            .HasColumnName("name");

        builder.Property(x => x.Description)
            .HasMaxLength(4000)
            .HasColumnType("text")
            .HasColumnName("description");

        builder.Property(x => x.DueDate)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("due_date");

        builder.Property(x => x.Priority)
            .HasConversion<int>()
            .HasDefaultValue(Priority.P4)
            .HasColumnName("priority");

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(TaskStatus.Open)
            .HasColumnName("status");

        builder.Property(x => x.SystemList)
            .HasConversion<int>()
            .HasDefaultValue(SystemList.Inbox)
            .HasColumnName("system_list");

        builder.Property(x => x.SortOrder)
            .HasDefaultValue(0)
            .HasColumnName("sort_order");

        builder.Property(x => x.ProjectId)
            .HasColumnType("uuid")
            .HasColumnName("project_id");

        builder.Property(x => x.IsArchived)
            .HasDefaultValue(false)
            .HasColumnName("is_archived");

        builder.Property(x => x.CompletedAt)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("completed_at");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("created_at")
            .ValueGeneratedNever();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("updated_at")
            .ValueGeneratedNever();

        // Foreign keys and relationships
        builder.HasOne(x => x.User)
            .WithMany(u => u.Tasks)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Labels)
            .WithMany(l => l.Tasks)
            .UsingEntity<TaskLabel>(
                l => l.HasOne<Label>().WithMany().HasForeignKey(tl => tl.LabelId),
                r => r.HasOne<TodoTask>().WithMany().HasForeignKey(tl => tl.TaskId),
                j => j.ToTable("task_labels"));

        // Indexes
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_tasks_userid");
        builder.HasIndex(x => x.ProjectId).HasDatabaseName("ix_tasks_projectid");
        builder.HasIndex(x => x.SystemList).HasDatabaseName("ix_tasks_systemlist");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_tasks_status");
        builder.HasIndex(x => x.DueDate).HasDatabaseName("ix_tasks_duedate");

        builder.ToTable("tasks");
    }
}
