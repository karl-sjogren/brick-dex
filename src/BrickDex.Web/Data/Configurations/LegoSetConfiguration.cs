using BrickDex.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrickDex.Web.Data.Configurations;

public class LegoSetConfiguration : IEntityTypeConfiguration<LegoSet> {
    public void Configure(EntityTypeBuilder<LegoSet> builder) {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.SetNumber)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(b => b.ThemeName)
            .HasMaxLength(256);

        builder.Property(b => b.ImageUrl)
            .HasMaxLength(1024);

        builder.Property(b => b.SetUrl)
            .HasMaxLength(1024);

        builder.Property(b => b.Notes)
            .HasMaxLength(4096);

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .IsRequired();

        builder.HasIndex(b => b.SetNumber);
    }
}
