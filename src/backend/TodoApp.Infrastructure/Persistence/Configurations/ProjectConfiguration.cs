using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Enums;

namespace TodoApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Project entity.
/// </summary>
public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
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
            .HasMaxLength(100)
            .HasColumnType("text")
            .HasColumnName("name");

        builder.Property(x => x.Description)
            .HasMaxLength(4000)
            .HasColumnType("text")
            .HasColumnName("description");

        builder.Property(x => x.DueDate)
            .HasColumnType("timestamp with time zone")
            .HasColumnName("due_date");

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(ProjectStatus.Active)
            .HasColumnName("status");

        builder.Property(x => x.SortOrder)
            .HasDefaultValue(0)
            .HasColumnName("sort_order");

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
            .WithMany(u => u.Projects)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_projects_userid");

        builder.ToTable("projects");
    }
}
