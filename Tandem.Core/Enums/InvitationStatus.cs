namespace Tandem.Core.Enums;

// Pending must stay 0: the unique index on GroupInvitations filters on "Status" = 0
public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Cancelled = 3
}
