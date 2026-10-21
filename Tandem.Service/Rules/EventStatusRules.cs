using Microsoft.Extensions.Logging;
using Tandem.Core.Entities;
using Tandem.Core.Enums;

namespace Tandem.Service.Rules;

// Organizing <-> Organized is never set by hand: every action that can change it
// calls Refresh just before saving, so the status is saved together with the change itself
internal static class EventStatusRules
{
    // Needs the event with venue, gift, meal, participants and items loaded
    public static void Refresh(Event evt, ILogger logger)
    {
        // Draft (no organizer yet), Settled and Cancelled do not change automatically
        if (evt.Status is not (EventStatus.Organizing or EventStatus.Organized))
        {
            return;
        }

        var newStatus = IsReady(evt) ? EventStatus.Organized : EventStatus.Organizing;
        if (newStatus != evt.Status)
        {
            evt.Status = newStatus;
            logger.LogInformation("Event {EventId} is now {Status}", evt.Id, newStatus);
        }
    }

    private static bool IsReady(Event evt) =>
        evt.OrganizerId is not null
        && IsCovered(evt.Venue)
        && IsCovered(evt.Meal)
        && (!evt.HasGift || IsCovered(evt.Gift))
        // The cook's private menu (SelfMade) is not waiting for anyone
        && evt.Items.All(i => i.IsPrivate || i.AssigneeId is not null)
        && (evt.SplitMethod != SplitMethod.SelectedMembers || evt.Participants.Count > 0);

    // Someone took the area and entered its price (0 counts: it is a price)
    private static bool IsCovered(IResponsibility? area) =>
        area is { ResponsibleId: not null, Price: not null };
}
