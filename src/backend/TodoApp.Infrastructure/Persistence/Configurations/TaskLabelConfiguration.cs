using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the TaskLabel join entity.
/// </summary>
public class TaskLabelConfiguration : IEntityTypeConfiguration<TaskLabel>
{
    public void Configure(EntityTypeBuilder<TaskLabel> builder)
    {
        builder.HasKey(x => new { x.TaskId, x.LabelId });

        builder.Property(x => x.TaskId)
            .HasColumnName("task_id")
            .HasColumnType("uuid");

        builder.Property(x => x.LabelId)
            .HasColumnName("label_id")
            .HasColumnType("uuid");

        // Foreign keys
        builder.HasOne(x => x.Task)
            .WithMany()
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Label)
            .WithMany()
            .HasForeignKey(x => x.LabelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("task_labels");
    }
}
