using BrickDex.Core.Models.Rebrickable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrickDex.Core.Data.Configurations;

public class RebrickableSetConfiguration : IEntityTypeConfiguration<RebrickableSet> {
    public void Configure(EntityTypeBuilder<RebrickableSet> builder) {
        builder.ToTable("RebrickableSets");

        builder.HasKey(s => s.SetNum);

        builder.Property(s => s.SetNum)
            .HasMaxLength(20);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(s => s.ImageUrl)
            .HasMaxLength(512);

        builder.HasMany(s => s.Inventories)
            .WithOne(i => i.Set)
            .HasForeignKey(i => i.SetNum)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.InventorySets)
            .WithOne(i => i.Set)
            .HasForeignKey(i => i.SetNum)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
