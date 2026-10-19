using Microsoft.EntityFrameworkCore;
using Tandem.Core.Entities;
using Tandem.Core.Enums;
using Tandem.Core.Interfaces.Repositories;

namespace Tandem.Data.Repositories;

public class InvitationRepository : IInvitationRepository
{
    private readonly AppDbContext _context;

    public InvitationRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<GroupInvitation>> GetPendingForEmailsAsync(
        int groupId, IReadOnlyCollection<string> emails, CancellationToken cancellationToken) =>
        _context.GroupInvitations
            .Where(i => i.GroupId == groupId
                        && i.Status == InvitationStatus.Pending
                        && emails.Contains(i.Email))
            .ToListAsync(cancellationToken);

    public Task<List<GroupInvitation>> GetForGroupAsync(int groupId, CancellationToken cancellationToken) =>
        _context.GroupInvitations
            .AsNoTracking()
            .Include(i => i.Group)
            .Include(i => i.InvitedBy)
            .Where(i => i.GroupId == groupId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

    public void AddRange(IEnumerable<GroupInvitation> invitations) =>
        _context.GroupInvitations.AddRange(invitations);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
