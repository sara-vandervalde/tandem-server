using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tandem.Core.Entities;

namespace Tandem.Data.Configurations;

public class EventParticipantConfiguration : IEntityTypeConfiguration<EventParticipant>
{
    public void Configure(EntityTypeBuilder<EventParticipant> builder)
    {
        // Composite key: a user is selected at most once per event
        builder.HasKey(ep => new { ep.EventId, ep.UserId });

        builder.HasOne(ep => ep.Event)
            .WithMany(e => e.Participants)
            .HasForeignKey(ep => ep.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ep => ep.User)
            .WithMany()
            .HasForeignKey(ep => ep.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
