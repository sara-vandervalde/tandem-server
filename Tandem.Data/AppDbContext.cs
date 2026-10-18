using Microsoft.EntityFrameworkCore;
using Tandem.Core.Entities;
using Tandem.Data.Seed;

namespace Tandem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<GroupInvitation> GroupInvitations => Set<GroupInvitation>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Gift> Gifts => Set<Gift>();
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<EventItem> EventItems => Set<EventItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Loads every IEntityTypeConfiguration class in this project (the Configurations folder)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Seed();
    }
}
