using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tandem.Core.Entities;

namespace Tandem.Data.Configurations;

public class GroupInvitationConfiguration : IEntityTypeConfiguration<GroupInvitation>
{
    public void Configure(EntityTypeBuilder<GroupInvitation> builder)
    {
        builder.Property(i => i.Email)
            .IsRequired()
            .HasMaxLength(256);

        // Only one pending invitation per email in a group, even if "invite" is clicked twice at once.
        // Status 0 = Pending (see InvitationStatus)
        builder.HasIndex(i => new { i.GroupId, i.Email })
            .IsUnique()
            .HasFilter("\"Status\" = 0");

        builder.HasOne(i => i.Group)
            .WithMany()
            .HasForeignKey(i => i.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.InvitedBy)
            .WithMany()
            .HasForeignKey(i => i.InvitedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
