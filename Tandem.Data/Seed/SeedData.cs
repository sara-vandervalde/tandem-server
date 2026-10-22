using Microsoft.EntityFrameworkCore;
using Tandem.Core.Entities;
using Tandem.Core.Enums;

namespace Tandem.Data.Seed;

// Demo data, inserted by the first migration so the system can be used right after the database is created.
// HasData does not run code, so every value is a constant: fixed UTC dates and pre-computed password hashes
public static class SeedData
{
    // Demo passwords (documented in the README): admin - Admin123!, members - Member123!
    private const string AdminPasswordHash =
        "AQAAAAIAAYagAAAAEAPLIbpQGPVGNBKCMyJkgj1+EnKCYNmz71Y9lVeAp1regLJ8owdthcZRvLGnCbX5Gw==";
    private const string MemberPasswordHash =
        "AQAAAAIAAYagAAAAELB6WKg+DL0UxsyGp6JC+CLQpnk0rjJPmE8mvpdYxeukRHCUVhtWY5MsiDBKTj9Zng==";

    private static readonly DateTime CreatedAt = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void Seed(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1, FullName = "מנהל המערכת", Email = "admin@example.com",
                PasswordHash = AdminPasswordHash, Role = UserRole.Admin, IsActive = true, CreatedAt = CreatedAt
            },
            new User
            {
                Id = 2, FullName = "שרה כהן", Email = "sara@example.com",
                PasswordHash = MemberPasswordHash, Role = UserRole.Member, IsActive = true, CreatedAt = CreatedAt
            },
            new User
            {
                Id = 3, FullName = "רחל לוי", Email = "rachel@example.com",
                PasswordHash = MemberPasswordHash, Role = UserRole.Member, IsActive = true, CreatedAt = CreatedAt
            });

        modelBuilder.Entity<Group>().HasData(
            new Group { Id = 1, Name = "המשפחה", ManagerId = 2, CreatedAt = CreatedAt });

        // The manager is always also a member
        modelBuilder.Entity<GroupMember>().HasData(
            new GroupMember { GroupId = 1, UserId = 2, JoinedAt = CreatedAt },
            new GroupMember { GroupId = 1, UserId = 3, JoinedAt = CreatedAt });

        modelBuilder.Entity<Event>().HasData(
            new Event
            {
                Id = 1, GroupId = 1, CreatedById = 2, Title = "יום ההולדת של סבתא",
                EventDate = new DateTime(2026, 12, 20, 17, 0, 0, DateTimeKind.Utc),
                OrganizerId = 2, OrganizationMode = OrganizationMode.Open,
                HasGift = false, SplitMethod = SplitMethod.Equal, Status = EventStatus.Organizing
            });

        // Venue and meal are created together with the event, still unclaimed
        modelBuilder.Entity<Venue>().HasData(new Venue { Id = 1, EventId = 1 });
        modelBuilder.Entity<Meal>().HasData(new Meal { Id = 1, EventId = 1 });

        // The banks operating in Israel, for the bank account form (added with the AddBanksAndSettlements migration)
        modelBuilder.Entity<Bank>().HasData(
            new Bank { Id = 1, Name = "בנק לאומי" },
            new Bank { Id = 2, Name = "בנק הפועלים" },
            new Bank { Id = 3, Name = "בנק דיסקונט" },
            new Bank { Id = 4, Name = "בנק מזרחי טפחות" },
            new Bank { Id = 5, Name = "הבנק הבינלאומי הראשון" },
            new Bank { Id = 6, Name = "בנק מרכנתיל דיסקונט" },
            new Bank { Id = 7, Name = "בנק ירושלים" },
            new Bank { Id = 8, Name = "בנק מסד" },
            new Bank { Id = 9, Name = "בנק יהב" },
            new Bank { Id = 10, Name = "בנק הדואר" },
            new Bank { Id = 11, Name = "וואן זירו – הבנק הדיגיטלי" });
    }
}
