using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tandem.Core.Entities;

namespace Tandem.Data.Configurations;

public class MealConfiguration : IEntityTypeConfiguration<Meal>
{
    public void Configure(EntityTypeBuilder<Meal> builder)
    {
        builder.Property(m => m.CateringName)
            .HasMaxLength(100);

        builder.Property(m => m.Price)
            .HasPrecision(18, 2);

        builder.Property(m => m.Version)
            .IsRowVersion();

        builder.HasOne(m => m.Event)
            .WithOne(e => e.Meal)
            .HasForeignKey<Meal>(m => m.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Responsible)
            .WithMany()
            .HasForeignKey(m => m.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
