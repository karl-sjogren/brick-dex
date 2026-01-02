using BrickDex.Core.Models.Rebrickable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrickDex.Core.Data.Configurations;

public class RebrickableThemeConfiguration : IEntityTypeConfiguration<RebrickableTheme> {
    public void Configure(EntityTypeBuilder<RebrickableTheme> builder) {
        builder.ToTable("RebrickableThemes");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasOne(t => t.Parent)
            .WithMany(t => t.Children)
            .HasForeignKey(t => t.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Sets)
            .WithOne(s => s.Theme)
            .HasForeignKey(s => s.ThemeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
