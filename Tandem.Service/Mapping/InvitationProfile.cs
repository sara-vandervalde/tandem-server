using AutoMapper;
using Tandem.Core.DTOs.Invitations;
using Tandem.Core.Entities;
using Tandem.Core.Enums;

namespace Tandem.Service.Mapping;

public class InvitationProfile : Profile
{
    public InvitationProfile()
    {
        CreateMap<GroupInvitation, InvitationDto>()
            .ForMember(d => d.GroupName, o => o.MapFrom(s => s.Group.Name))
            .ForMember(d => d.InvitedByName, o => o.MapFrom(s => s.InvitedBy.FullName))
            // Expiry is not a stored status: it is a pending invitation whose date has passed
            .ForMember(d => d.IsExpired, o => o.MapFrom(s =>
                s.Status == InvitationStatus.Pending && s.ExpiresAt <= DateTime.UtcNow));
    }
}
