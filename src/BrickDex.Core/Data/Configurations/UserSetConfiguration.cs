using BrickDex.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrickDex.Core.Data.Configurations;

public class UserSetConfiguration : IEntityTypeConfiguration<UserSet> {
    public void Configure(EntityTypeBuilder<UserSet> builder) {
        builder.HasKey(us => us.Id);

        builder.Property(us => us.SetNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(us => us.Notes)
            .HasMaxLength(4096);

        builder.Property(us => us.CreatedAt)
            .IsRequired();

        builder.Property(us => us.UpdatedAt)
            .IsRequired();

        builder.HasIndex(us => new { us.UserId, us.SetNumber })
            .IsUnique();

        builder.HasOne(us => us.Set)
            .WithMany(s => s.UserSets)
            .HasForeignKey(us => us.SetNumber)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
