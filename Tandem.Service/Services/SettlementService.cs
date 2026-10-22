using AutoMapper;
using Microsoft.Extensions.Logging;
using Tandem.Core.Common;
using Tandem.Core.DTOs.Banks;
using Tandem.Core.DTOs.Settlements;
using Tandem.Core.Entities;
using Tandem.Core.Enums;
using Tandem.Core.Exceptions;
using Tandem.Core.Interfaces.Repositories;
using Tandem.Core.Interfaces.Services;
using Tandem.Service.Settlements;

namespace Tandem.Service.Services;

public class SettlementService : ISettlementService
{
    private const string EventNotFoundMessage = "האירוע לא נמצא";

    private readonly ISettlementRepository _settlementRepository;
    private readonly IBankRepository _bankRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SettlementService> _logger;

    public SettlementService(
        ISettlementRepository settlementRepository,
        IBankRepository bankRepository,
        IMapper mapper,
        ILogger<SettlementService> logger)
    {
        _settlementRepository = settlementRepository;
        _bankRepository = bankRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<SettlementPreviewDto>> GetPreviewAsync(
        int eventId, int userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var evt = await _settlementRepository.GetEventForPreviewAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return Result<SettlementPreviewDto>.NotFound(EventNotFoundMessage);
        }

        if (!isAdmin && evt.Group.Members.All(m => m.UserId != userId))
        {
            return Result<SettlementPreviewDto>.Forbidden("אין לך גישה לאירוע זה");
        }

        var payments = GetPayments(evt);
        var sharerIds = GetSharerIds(evt);
        var names = GetNames(evt);

        // SelectedMembers before anyone was chosen: nothing to split yet
        var transfers = sharerIds.Count == 0
            ? []
            : SettlementCalculator.Calculate(sharerIds, payments);

        return Result<SettlementPreviewDto>.Success(new SettlementPreviewDto
        {
            Total = payments.Sum(p => p.Amount),
            SharerCount = sharerIds.Count,
            Transfers = transfers.Select(t => new TransferDto
            {
                FromUserId = t.FromUserId,
                FromUserName = names.GetValueOrDefault(t.FromUserId, string.Empty),
                ToUserId = t.ToUserId,
                ToUserName = names.GetValueOrDefault(t.ToUserId, string.Empty),
                Amount = t.Amount
            }).ToList()
        });
    }

    public async Task<Result<IReadOnlyList<SettlementDto>>> CloseAsync(
        int eventId, int userId, CancellationToken cancellationToken)
    {
        // Tracked: the event's version (xmin) protects against closing twice at the same moment
        var evt = await _settlementRepository.GetEventForCloseAsync(eventId, cancellationToken);
        if (evt is null)
        {
            return Result<IReadOnlyList<SettlementDto>>.NotFound(EventNotFoundMessage);
        }

        if (evt.OrganizerId != userId)
        {
            return Result<IReadOnlyList<SettlementDto>>.Forbidden("רק אחראי/ת האירוע יכול/ה לסגור את החשבון");
        }

        if (evt.Status == EventStatus.Settled)
        {
            return Result<IReadOnlyList<SettlementDto>>.Conflict("החשבון של האירוע הזה כבר נסגר");
        }

        // Organized = every area has a responsible member and a price, every public item is claimed
        if (evt.Status != EventStatus.Organized)
        {
            return Result<IReadOnlyList<SettlementDto>>.Conflict(
                "אפשר לסגור חשבון רק כשהאירוע מאורגן: לכל התחומים יש אחראי/ת ומחיר, וכל הפריטים נתפסו");
        }

        var users = GetUsers(evt);
        var transfers = SettlementCalculator.Calculate(GetSharerIds(evt), GetPayments(evt));
        var settlements = transfers.Select(t => new Settlement
        {
            EventId = eventId,
            FromUserId = t.FromUserId,
            FromUser = users[t.FromUserId],
            ToUserId = t.ToUserId,
            ToUser = users[t.ToUserId],
            Amount = t.Amount
        }).ToList();

        // One save: the payments and the status change are written together.
        // From now on the prices are locked - every price update checks for Settled and answers 409
        _settlementRepository.AddRange(settlements);
        evt.Status = EventStatus.Settled;

        try
        {
            await _settlementRepository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Concurrency conflict: event {EventId} changed while user {UserId} was closing its account",
                eventId, userId);
            return Result<IReadOnlyList<SettlementDto>>.Conflict("האירוע השתנה בינתיים. רעננו את הדף ונסו שוב");
        }

        _logger.LogInformation("Event {EventId} settled by user {UserId}: {Count} payments created",
            eventId, userId, settlements.Count);

        return Result<IReadOnlyList<SettlementDto>>.Success(_mapper.Map<List<SettlementDto>>(settlements));
    }

    public async Task<IReadOnlyList<MySettlementDto>> GetMineAsync(int userId, CancellationToken cancellationToken)
    {
        var settlements = await _settlementRepository.GetForUserAsync(userId, cancellationToken);

        // Bank details are shown only to someone who owes the account holder money:
        // load the accounts of the people I pay - all of them in one query
        var recipientIds = settlements
            .Where(s => s.FromUserId == userId)
            .Select(s => s.ToUserId)
            .Distinct()
            .ToList();
        var accounts = recipientIds.Count == 0
            ? new Dictionary<int, BankAccountDto>()
            : (await _bankRepository.GetAccountsAsync(recipientIds, cancellationToken))
                .ToDictionary(a => a.UserId, a => _mapper.Map<BankAccountDto>(a));

        var result = new List<MySettlementDto>();
        foreach (var settlement in settlements)
        {
            var dto = _mapper.Map<MySettlementDto>(settlement);
            dto.IOwe = settlement.FromUserId == userId;
            dto.RecipientBankAccount = dto.IOwe ? accounts.GetValueOrDefault(settlement.ToUserId) : null;
            result.Add(dto);
        }

        return result;
    }

    public async Task<Result> SetPaidAsync(int settlementId, bool isPaid, int userId, CancellationToken cancellationToken)
    {
        var settlement = await _settlementRepository.GetForUpdateAsync(settlementId, cancellationToken);

        // Someone else's payment answers 404: for this user it does not exist
        if (settlement is null || (settlement.FromUserId != userId && settlement.ToUserId != userId))
        {
            return Result.NotFound("התשלום לא נמצא");
        }

        if (settlement.FromUserId != userId)
        {
            return Result.Forbidden("רק מי שמשלם/ת יכול/ה לסמן שהתשלום בוצע");
        }

        // Marking twice is not an error (PATCH with the same value gives the same result)
        if (settlement.IsPaid != isPaid)
        {
            settlement.IsPaid = isPaid;
            settlement.PaidAt = isPaid ? DateTime.UtcNow : null;
            await _settlementRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Settlement {SettlementId} marked as {State} by user {UserId}",
                settlementId, isPaid ? "paid" : "unpaid", userId);
        }

        return Result.Success();
    }

    // Who shares the cost - the only thing the split method decides
    private static List<int> GetSharerIds(Event evt) => evt.SplitMethod switch
    {
        SplitMethod.OrganizerPays => [evt.OrganizerId!.Value],
        SplitMethod.SelectedMembers => evt.Participants.Select(p => p.UserId).ToList(),
        _ => evt.Group.Members.Select(m => m.UserId).ToList()
    };

    // What each responsible member paid. An area without a responsible member or a price is not counted yet
    private static List<Payment> GetPayments(Event evt)
    {
        IResponsibility?[] areas = [evt.Venue, evt.Meal, evt.HasGift ? evt.Gift : null];

        return areas
            .Where(a => a is { ResponsibleId: not null, Price: not null })
            .Select(a => new Payment(a!.ResponsibleId!.Value, a.Price!.Value))
            .ToList();
    }

    // Group members and participants (a participant is always a member, but the lists are loaded separately)
    private static Dictionary<int, User> GetUsers(Event evt) =>
        evt.Group.Members.Select(m => m.User)
            .Concat(evt.Participants.Select(p => p.User))
            .DistinctBy(u => u.Id)
            .ToDictionary(u => u.Id);

    private static Dictionary<int, string> GetNames(Event evt) =>
        GetUsers(evt).ToDictionary(pair => pair.Key, pair => pair.Value.FullName);
}
