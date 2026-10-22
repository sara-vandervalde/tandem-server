using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tandem.Core.Entities;

namespace Tandem.Data.Configurations;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.Property(a => a.BranchNumber)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(a => a.AccountNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.AccountHolderName)
            .IsRequired()
            .HasMaxLength(100);

        // One-to-one with no navigation on User: the unique index on UserId allows one account per user,
        // even if two saves arrive at the same moment
        builder.HasOne(a => a.User)
            .WithOne()
            .HasForeignKey<BankAccount>(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.UserId)
            .IsUnique();

        // A bank that has accounts cannot be removed from the list
        builder.HasOne(a => a.Bank)
            .WithMany()
            .HasForeignKey(a => a.BankId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
