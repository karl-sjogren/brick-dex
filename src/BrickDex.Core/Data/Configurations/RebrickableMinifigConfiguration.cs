using BrickDex.Core.Models.Rebrickable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrickDex.Core.Data.Configurations;

public class RebrickableMinifigConfiguration : IEntityTypeConfiguration<RebrickableMinifig> {
    public void Configure(EntityTypeBuilder<RebrickableMinifig> builder) {
        builder.ToTable("RebrickableMinifigs");

        builder.HasKey(m => m.FigNum);

        builder.Property(m => m.FigNum)
            .HasMaxLength(20);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(m => m.ImageUrl)
            .HasMaxLength(512);

        builder.HasMany(m => m.InventoryMinifigs)
            .WithOne(im => im.Minifig)
            .HasForeignKey(im => im.FigNum)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
