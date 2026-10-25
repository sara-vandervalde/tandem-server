using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tandem.Core.Entities;

namespace Tandem.Data.Configurations;

public class EventMessageConfiguration : IEntityTypeConfiguration<EventMessage>
{
    public void Configure(EntityTypeBuilder<EventMessage> builder)
    {
        builder.Property(m => m.Text)
            .HasMaxLength(500)
            .IsRequired();

        // The board is part of the event: deleting the event deletes its messages
        builder.HasOne(m => m.Event)
            .WithMany()
            .HasForeignKey(m => m.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // The board is always read "this event, newest first" - the index matches that query
        builder.HasIndex(m => new { m.EventId, m.CreatedAt });
    }
}
