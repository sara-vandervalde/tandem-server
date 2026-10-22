using Microsoft.Extensions.Logging.Abstractions;
using Tandem.Core.Entities;
using Tandem.Core.Enums;
using Tandem.Service.Rules;

namespace Tandem.Tests.Rules;

// The rule is a pure function of the event: no mocks are needed, only an event in memory
public class EventStatusRulesTests
{
    private const int Dana = 100;
    private const int Noa = 200;

    // Everything in place: organizer, venue and meal with a responsible member and a price, no gift
    private static Event ReadyEvent() => new()
    {
        Id = 10,
        OrganizerId = Dana,
        Status = EventStatus.Organizing,
        Venue = new Venue { ResponsibleId = Dana, Price = 300 },
        Meal = new Meal { ResponsibleId = Noa, Price = 200 }
    };

    private static void Refresh(Event evt) => EventStatusRules.Refresh(evt, NullLogger.Instance);

    [Fact]
    public void AllCovered_NoGift_BecomesOrganized()
    {
        var evt = ReadyEvent();

        Refresh(evt);

        Assert.Equal(EventStatus.Organized, evt.Status);
    }

    [Fact]
    public void PriceZero_CountsAsEntered()
    {
        var evt = ReadyEvent();
        evt.Venue.Price = 0;

        Refresh(evt);

        Assert.Equal(EventStatus.Organized, evt.Status);
    }

    [Fact]
    public void PriceMissing_StaysOrganizing()
    {
        var evt = ReadyEvent();
        evt.Meal.Price = null;

        Refresh(evt);

        Assert.Equal(EventStatus.Organizing, evt.Status);
    }

    [Fact]
    public void AreaWithoutResponsible_GoesBackToOrganizing()
    {
        var evt = ReadyEvent();
        evt.Status = EventStatus.Organized;
        evt.Venue.ResponsibleId = null;

        Refresh(evt);

        Assert.Equal(EventStatus.Organizing, evt.Status);
    }

    [Fact]
    public void HasGift_WithoutResponsible_StaysOrganizing()
    {
        var evt = ReadyEvent();
        evt.HasGift = true;
        evt.Gift = new Gift();

        Refresh(evt);

        Assert.Equal(EventStatus.Organizing, evt.Status);
    }

    [Fact]
    public void HasGift_Covered_BecomesOrganized()
    {
        var evt = ReadyEvent();
        evt.HasGift = true;
        evt.Gift = new Gift { ResponsibleId = Dana, Price = 150 };

        Refresh(evt);

        Assert.Equal(EventStatus.Organized, evt.Status);
    }

    [Fact]
    public void PublicItemNotClaimed_StaysOrganizing()
    {
        var evt = ReadyEvent();
        evt.Items.Add(new EventItem { Name = "Cake", IsPrivate = false, AssigneeId = null });

        Refresh(evt);

        Assert.Equal(EventStatus.Organizing, evt.Status);
    }

    [Fact]
    public void PrivateItemNotClaimed_DoesNotBlock()
    {
        // The cook's own menu (SelfMade): nobody is expected to claim it
        var evt = ReadyEvent();
        evt.Items.Add(new EventItem { Name = "Cake", IsPrivate = true, AssigneeId = null });

        Refresh(evt);

        Assert.Equal(EventStatus.Organized, evt.Status);
    }

    [Fact]
    public void SelectedMembers_WithoutParticipants_StaysOrganizing()
    {
        var evt = ReadyEvent();
        evt.SplitMethod = SplitMethod.SelectedMembers;

        Refresh(evt);

        Assert.Equal(EventStatus.Organizing, evt.Status);
    }

    [Fact]
    public void SelectedMembers_WithParticipant_BecomesOrganized()
    {
        var evt = ReadyEvent();
        evt.SplitMethod = SplitMethod.SelectedMembers;
        evt.Participants.Add(new EventParticipant { EventId = 10, UserId = Noa });

        Refresh(evt);

        Assert.Equal(EventStatus.Organized, evt.Status);
    }

    [Theory]
    [InlineData(EventStatus.Draft)]
    [InlineData(EventStatus.Settled)]
    [InlineData(EventStatus.Cancelled)]
    public void StatusesOutsideOrganizing_AreNeverChanged(EventStatus status)
    {
        var evt = ReadyEvent();
        evt.Status = status;

        Refresh(evt);

        Assert.Equal(status, evt.Status);
    }
}
