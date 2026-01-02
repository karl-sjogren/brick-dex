using BrickDex.Core.Models.Rebrickable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrickDex.Core.Data.Configurations;

public class RebrickableInventoryConfiguration : IEntityTypeConfiguration<RebrickableInventory> {
    public void Configure(EntityTypeBuilder<RebrickableInventory> builder) {
        builder.ToTable("RebrickableInventories");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedNever();

        builder.Property(i => i.SetNum)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasMany(i => i.InventoryMinifigs)
            .WithOne(im => im.Inventory)
            .HasForeignKey(im => im.InventoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.InventorySets)
            .WithOne(s => s.Inventory)
            .HasForeignKey(s => s.InventoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
