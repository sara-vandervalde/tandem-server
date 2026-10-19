using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Tandem.Core.Common;
using Tandem.Core.DTOs.Invitations;
using Tandem.Core.Entities;
using Tandem.Core.Enums;
using Tandem.Core.Interfaces;
using Tandem.Core.Interfaces.Repositories;
using Tandem.Core.Interfaces.Services;
using Tandem.Service.Authorization;

namespace Tandem.Service.Services;

public class InvitationService : IInvitationService
{
    private const int ValidityDays = 14;
    private const string InvitationNotFoundMessage = "ההזמנה לא נמצאה";

    private static readonly EmailAddressAttribute EmailValidator = new();

    private readonly IInvitationRepository _invitationRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly GroupAccessService _groupAccess;
    private readonly INotificationSender _notificationSender;
    private readonly IMapper _mapper;
    private readonly ILogger<InvitationService> _logger;

    public InvitationService(
        IInvitationRepository invitationRepository,
        IGroupRepository groupRepository,
        GroupAccessService groupAccess,
        INotificationSender notificationSender,
        IMapper mapper,
        ILogger<InvitationService> logger)
    {
        _invitationRepository = invitationRepository;
        _groupRepository = groupRepository;
        _groupAccess = groupAccess;
        _notificationSender = notificationSender;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<CreateInvitationsResponse>> CreateAsync(
        int groupId, CreateInvitationsRequest request, int userId, CancellationToken cancellationToken)
    {
        var access = await _groupAccess.EnsureManagerAsync(groupId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToFailure<CreateInvitationsResponse>();
        }
        var group = access.Value!;

        var skipped = new List<SkippedEmailDto>();
        var emails = NormalizeEmails(request.Emails, skipped);

        // Already in the group - nothing to invite
        var memberEmails = await _groupRepository.GetMemberEmailsAsync(groupId, emails, cancellationToken);
        skipped.AddRange(memberEmails.Select(e => Skip(e, "כבר חבר/ה בקבוצה")));
        var candidates = emails.Except(memberEmails).ToList();

        var now = DateTime.UtcNow;
        var expiresAt = now.AddDays(ValidityDays);
        var invitedEmails = new List<string>();

        // A pending invitation that is still valid is kept; an expired one is renewed instead of adding a second row
        var pending = await _invitationRepository.GetPendingForEmailsAsync(groupId, candidates, cancellationToken);
        foreach (var invitation in pending)
        {
            if (invitation.ExpiresAt > now)
            {
                skipped.Add(Skip(invitation.Email, "כבר נשלחה הזמנה שעדיין ממתינה"));
                continue;
            }

            invitation.InvitedById = userId;
            invitation.CreatedAt = now;
            invitation.ExpiresAt = expiresAt;
            invitedEmails.Add(invitation.Email);
        }

        var newInvitations = candidates
            .Except(pending.Select(i => i.Email))
            .Select(email => new GroupInvitation
            {
                GroupId = groupId,
                Email = email,
                InvitedById = userId,
                Status = InvitationStatus.Pending,
                CreatedAt = now,
                ExpiresAt = expiresAt
            })
            .ToList();
        invitedEmails.AddRange(newInvitations.Select(i => i.Email));

        _invitationRepository.AddRange(newInvitations);
        await _invitationRepository.SaveChangesAsync(cancellationToken);

        foreach (var email in invitedEmails)
        {
            await _notificationSender.SendAsync(
                email,
                $"הוזמנת לקבוצה \"{group.Name}\" ב-Tandem",
                $"הוזמנת להצטרף לקבוצה \"{group.Name}\". ההזמנה בתוקף עד {expiresAt:dd/MM/yyyy}. " +
                "היכנסו למערכת עם כתובת האימייל הזו כדי לאשר או לדחות.",
                cancellationToken);
        }

        _logger.LogInformation("{Count} invitations sent for group {GroupId} by user {UserId}",
            invitedEmails.Count, groupId, userId);

        var invited = (await _invitationRepository.GetForGroupAsync(groupId, cancellationToken))
            .Where(i => i.Status == InvitationStatus.Pending && invitedEmails.Contains(i.Email));

        return Result<CreateInvitationsResponse>.Success(new CreateInvitationsResponse
        {
            Invited = _mapper.Map<List<InvitationDto>>(invited),
            Skipped = skipped
        });
    }

    public async Task<Result<IReadOnlyList<InvitationDto>>> GetForGroupAsync(
        int groupId, int userId, CancellationToken cancellationToken)
    {
        var access = await _groupAccess.EnsureManagerAsync(groupId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToFailure<IReadOnlyList<InvitationDto>>();
        }

        var invitations = await _invitationRepository.GetForGroupAsync(groupId, cancellationToken);
        return Result<IReadOnlyList<InvitationDto>>.Success(_mapper.Map<List<InvitationDto>>(invitations));
    }

    public async Task<IReadOnlyList<InvitationDto>> GetMyInvitationsAsync(
        string userEmail, CancellationToken cancellationToken)
    {
        var invitations = await _invitationRepository.GetActiveForEmailAsync(
            userEmail.ToLowerInvariant(), DateTime.UtcNow, cancellationToken);

        return _mapper.Map<List<InvitationDto>>(invitations);
    }

    public async Task<Result> RespondAsync(
        int invitationId, bool accept, int userId, string userEmail, CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdForUpdateAsync(invitationId, cancellationToken);
        if (invitation is null)
        {
            return Result.NotFound(InvitationNotFoundMessage);
        }

        // An invitation belongs to an email address, not to a user id
        if (invitation.Email != userEmail.ToLowerInvariant())
        {
            return Result.Forbidden("ההזמנה הזו לא נשלחה אלייך");
        }

        var stateError = CheckStillPending(invitation, DateTime.UtcNow);
        if (stateError is not null)
        {
            return stateError;
        }

        invitation.Status = accept ? InvitationStatus.Accepted : InvitationStatus.Declined;
        invitation.RespondedAt = DateTime.UtcNow;

        // Joining and closing the invitation are one save: either both happen or neither does.
        // Both repositories share the request's DbContext, so one SaveChanges writes both rows
        var alreadyMember = await _groupRepository.IsMemberAsync(invitation.GroupId, userId, cancellationToken);
        if (accept && !alreadyMember)
        {
            _groupRepository.AddMember(new GroupMember
            {
                GroupId = invitation.GroupId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });
        }

        await _invitationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Invitation {InvitationId} {Action} by user {UserId}",
            invitationId, accept ? "accepted" : "declined", userId);

        if (accept)
        {
            await _notificationSender.SendAsync(
                invitation.Group.Manager.Email,
                $"חבר/ה חדש/ה הצטרף/ה לקבוצה \"{invitation.Group.Name}\"",
                $"{invitation.Email} אישר/ה את ההזמנה והצטרף/ה לקבוצה \"{invitation.Group.Name}\".",
                cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> CancelAsync(int invitationId, int userId, CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdForUpdateAsync(invitationId, cancellationToken);
        if (invitation is null)
        {
            return Result.NotFound(InvitationNotFoundMessage);
        }

        if (invitation.Group.ManagerId != userId)
        {
            return Result.Forbidden("רק מנהל הקבוצה יכול לבטל הזמנה");
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            return Result.Conflict("אפשר לבטל רק הזמנה שעדיין ממתינה");
        }

        // Kept as Cancelled rather than deleted, so the manager still sees the history
        invitation.Status = InvitationStatus.Cancelled;
        invitation.RespondedAt = DateTime.UtcNow;
        await _invitationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Invitation {InvitationId} cancelled by user {UserId}", invitationId, userId);

        return Result.Success();
    }

    private static Result? CheckStillPending(GroupInvitation invitation, DateTime now)
    {
        if (invitation.Status != InvitationStatus.Pending)
        {
            return Result.Conflict("ההזמנה כבר טופלה");
        }

        return invitation.ExpiresAt <= now
            ? Result.Conflict("תוקף ההזמנה פג. אפשר לבקש ממנהל הקבוצה הזמנה חדשה")
            : null;
    }

    // Lowercase, trim, drop duplicates; invalid addresses go to the skipped list
    private static List<string> NormalizeEmails(IEnumerable<string> rawEmails, List<SkippedEmailDto> skipped)
    {
        var emails = new List<string>();
        foreach (var raw in rawEmails)
        {
            var email = (raw ?? string.Empty).Trim().ToLowerInvariant();

            if (!EmailValidator.IsValid(email) || email.Length > 256)
            {
                skipped.Add(Skip(raw ?? string.Empty, "כתובת אימייל לא תקינה"));
            }
            else if (!emails.Contains(email))
            {
                emails.Add(email);
            }
        }
        return emails;
    }

    private static SkippedEmailDto Skip(string email, string reason) => new() { Email = email, Reason = reason };
}
