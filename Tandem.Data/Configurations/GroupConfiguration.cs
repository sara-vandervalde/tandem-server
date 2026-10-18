using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tandem.Core.Entities;

namespace Tandem.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Restrict: a user who manages a group cannot be deleted by accident
        builder.HasOne(g => g.Manager)
            .WithMany()
            .HasForeignKey(g => g.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
