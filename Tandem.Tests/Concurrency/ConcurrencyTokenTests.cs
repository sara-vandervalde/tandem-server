using Microsoft.EntityFrameworkCore;
using Tandem.Core.Entities;
using Tandem.Core.Enums;
using Tandem.Core.Exceptions;
using Tandem.Data.Repositories;

namespace Tandem.Tests.Concurrency;

// The proof that the concurrency token works: two separate DbContexts (= two members clicking
// "I'll take it" at the same moment) read the same row, both change it, the first save succeeds
// and the second one is rejected - against a real PostgreSQL database
public class ConcurrencyTokenTests : IClassFixture<TestDatabaseFixture>
{
    // From the Seed: group 1 "המשפחה" with Sara (2) and Rachel (3)
    private const int GroupId = 1;
    private const int Sara = 2;
    private const int Rachel = 3;

    private readonly TestDatabaseFixture _database;

    public ConcurrencyTokenTests(TestDatabaseFixture database)
    {
        _database = database;
    }

    // A fresh event for every test, so the tests never depend on each other
    private async Task<int> CreateEventAsync()
    {
        await using var context = _database.CreateContext();
        var evt = new Event
        {
            GroupId = GroupId,
            CreatedById = Sara,
            OrganizerId = Sara,
            Title = "Concurrency test",
            EventDate = DateTime.UtcNow.AddDays(30),
            Status = EventStatus.Organizing,
            Venue = new Venue(),
            Meal = new Meal()
        };
        context.Events.Add(evt);
        await context.SaveChangesAsync();
        return evt.Id;
    }

    [Fact]
    public async Task TwoContexts_ClaimSameVenue_FirstSucceeds_SecondThrows()
    {
        var eventId = await CreateEventAsync();

        // 1. Both read the same free venue - each one remembers the row's version (xmin)
        await using var saraContext = _database.CreateContext();
        await using var rachelContext = _database.CreateContext();
        var saraVenue = await saraContext.Venues.SingleAsync(v => v.EventId == eventId);
        var rachelVenue = await rachelContext.Venues.SingleAsync(v => v.EventId == eventId);
        Assert.Null(saraVenue.ResponsibleId);
        Assert.Null(rachelVenue.ResponsibleId);

        // 2. Both claim it
        saraVenue.ResponsibleId = Sara;
        rachelVenue.ResponsibleId = Rachel;

        // 3. The first save wins: UPDATE ... WHERE "Id" = @id AND xmin = @version -> 1 row
        await saraContext.SaveChangesAsync();

        // 4. The second save: the version changed, the UPDATE matches 0 rows -> exception
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => rachelContext.SaveChangesAsync());

        // 5. The database keeps the winner
        await using var checkContext = _database.CreateContext();
        var saved = await checkContext.Venues.AsNoTracking().SingleAsync(v => v.EventId == eventId);
        Assert.Equal(Sara, saved.ResponsibleId);
    }

    [Fact]
    public async Task TwoRepositories_ClaimSameVenue_SecondGetsConcurrencyConflictException()
    {
        // The same race through the real repository: it translates EF's exception to Core's one,
        // which the service turns into 409
        var eventId = await CreateEventAsync();

        await using var saraContext = _database.CreateContext();
        await using var rachelContext = _database.CreateContext();
        var saraRepository = new EventRepository(saraContext);
        var rachelRepository = new EventRepository(rachelContext);

        var saraEvent = await saraRepository.GetForUpdateAsync(eventId, CancellationToken.None);
        var rachelEvent = await rachelRepository.GetForUpdateAsync(eventId, CancellationToken.None);
        saraEvent!.Venue.ResponsibleId = Sara;
        rachelEvent!.Venue.ResponsibleId = Rachel;

        await saraRepository.SaveChangesAsync(CancellationToken.None);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => rachelRepository.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task TwoContexts_ClaimDifferentAreas_BothSucceed()
    {
        // The token is per row: claiming the venue and the meal at the same moment is not a conflict
        var eventId = await CreateEventAsync();

        await using var saraContext = _database.CreateContext();
        await using var rachelContext = _database.CreateContext();
        var venue = await saraContext.Venues.SingleAsync(v => v.EventId == eventId);
        var meal = await rachelContext.Meals.SingleAsync(m => m.EventId == eventId);

        venue.ResponsibleId = Sara;
        meal.ResponsibleId = Rachel;

        await saraContext.SaveChangesAsync();
        await rachelContext.SaveChangesAsync();

        await using var checkContext = _database.CreateContext();
        Assert.Equal(Sara, (await checkContext.Venues.SingleAsync(v => v.EventId == eventId)).ResponsibleId);
        Assert.Equal(Rachel, (await checkContext.Meals.SingleAsync(m => m.EventId == eventId)).ResponsibleId);
    }
}
