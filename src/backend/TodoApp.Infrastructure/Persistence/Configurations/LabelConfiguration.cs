using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Label entity.
/// </summary>
public class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("text");

        builder.Property(x => x.Color)
            .HasMaxLength(7)
            .HasColumnType("text");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .ValueGeneratedNever();

        // Unique constraint on Name per user
        builder.HasIndex(x => new { x.UserId, x.Name })
            .IsUnique()
            .HasDatabaseName("ix_labels_userid_name_unique");

        // Foreign keys and relationships
        builder.HasOne(x => x.User)
            .WithMany(u => u.Labels)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_labels_userid");

        builder.ToTable("labels");
    }
}
