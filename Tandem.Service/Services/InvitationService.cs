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
