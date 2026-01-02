using BrickDex.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrickDex.Core.Data.Configurations;

public class UserLoginConfiguration : IEntityTypeConfiguration<UserLogin> {
    public void Configure(EntityTypeBuilder<UserLogin> builder) {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Provider)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(l => l.ProviderKey)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(l => l.CreatedAt)
            .IsRequired();

        builder.Property(l => l.UpdatedAt)
            .IsRequired();

        builder.HasIndex(l => new { l.Provider, l.ProviderKey })
            .IsUnique();
    }
}
