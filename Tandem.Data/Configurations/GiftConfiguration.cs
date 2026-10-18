using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tandem.Core.Entities;

namespace Tandem.Data.Configurations;

public class GiftConfiguration : IEntityTypeConfiguration<Gift>
{
    public void Configure(EntityTypeBuilder<Gift> builder)
    {
        builder.Property(g => g.Description)
            .HasMaxLength(200);

        builder.Property(g => g.Price)
            .HasPrecision(18, 2);

        builder.Property(g => g.Version)
            .IsRowVersion();

        builder.HasOne(g => g.Event)
            .WithOne(e => e.Gift)
            .HasForeignKey<Gift>(g => g.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.Responsible)
            .WithMany()
            .HasForeignKey(g => g.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
