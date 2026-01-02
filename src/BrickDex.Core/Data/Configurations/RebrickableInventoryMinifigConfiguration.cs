using BrickDex.Core.Models.Rebrickable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrickDex.Core.Data.Configurations;

public class RebrickableInventoryMinifigConfiguration : IEntityTypeConfiguration<RebrickableInventoryMinifig> {
    public void Configure(EntityTypeBuilder<RebrickableInventoryMinifig> builder) {
        builder.ToTable("RebrickableInventoryMinifigs");

        builder.HasKey(im => new { im.InventoryId, im.FigNum });

        builder.Property(im => im.FigNum)
            .HasMaxLength(20);
    }
}
