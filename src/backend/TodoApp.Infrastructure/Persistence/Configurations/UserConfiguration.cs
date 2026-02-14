using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the User entity.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnType("uuid")
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("email")
            .HasColumnType("text");

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("ix_users_email_unique");

        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasColumnName("password_hash")
            .HasColumnType("text");

        builder.Property(x => x.PasswordSalt)
            .IsRequired()
            .HasColumnName("password_salt")
            .HasColumnType("text");

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("display_name")
            .HasColumnType("text");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .ValueGeneratedNever();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .ValueGeneratedNever();

        builder.ToTable("users");
    }
}
