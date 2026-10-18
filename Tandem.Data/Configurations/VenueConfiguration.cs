using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tandem.Core.Entities;

namespace Tandem.Data.Configurations;

public class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.Property(v => v.Address)
            .HasMaxLength(200);

        builder.Property(v => v.Price)
            .HasPrecision(18, 2);

        builder.Property(v => v.Version)
            .IsRowVersion();

        // One-to-one: WithOne creates a unique index on EventId, so an event can never get a second venue
        builder.HasOne(v => v.Event)
            .WithOne(e => e.Venue)
            .HasForeignKey<Venue>(v => v.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Responsible)
            .WithMany()
            .HasForeignKey(v => v.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
