using BrickDex.Core.Models.Rebrickable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrickDex.Core.Data.Configurations;

public class RebrickableInventorySetConfiguration : IEntityTypeConfiguration<RebrickableInventorySet> {
    public void Configure(EntityTypeBuilder<RebrickableInventorySet> builder) {
        builder.ToTable("RebrickableInventorySets");

        builder.HasKey(s => new { s.InventoryId, s.SetNum });

        builder.Property(s => s.SetNum)
            .HasMaxLength(20);
    }
}
