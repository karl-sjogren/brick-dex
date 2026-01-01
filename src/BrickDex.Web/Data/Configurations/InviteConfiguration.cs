using BrickDex.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrickDex.Web.Data.Configurations;

public class InviteConfiguration : IEntityTypeConfiguration<Invite> {
    public void Configure(EntityTypeBuilder<Invite> builder) {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Code)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(i => i.ExpiresAt)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .IsRequired();

        // Unique index on Code for fast lookup
        builder.HasIndex(i => i.Code)
            .IsUnique();

        // Index for finding invites by creator
        builder.HasIndex(i => i.CreatedByUserId);

        // Relationship: CreatedBy
        builder.HasOne(i => i.CreatedByUser)
            .WithMany()
            .HasForeignKey(i => i.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship: UsedBy
        builder.HasOne(i => i.UsedByUser)
            .WithMany()
            .HasForeignKey(i => i.UsedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
