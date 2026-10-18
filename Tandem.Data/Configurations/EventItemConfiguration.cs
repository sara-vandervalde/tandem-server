using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tandem.Core.Entities;

namespace Tandem.Data.Configurations;

public class EventItemConfiguration : IEntityTypeConfiguration<EventItem>
{
    public void Configure(EntityTypeBuilder<EventItem> builder)
    {
        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.Version)
            .IsRowVersion();

        builder.HasOne(i => i.Event)
            .WithMany(e => e.Items)
            .HasForeignKey(i => i.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.AddedBy)
            .WithMany()
            .HasForeignKey(i => i.AddedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Assignee)
            .WithMany()
            .HasForeignKey(i => i.AssigneeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
