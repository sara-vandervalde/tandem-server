using AutoMapper;
using Tandem.Core.DTOs.Settlements;
using Tandem.Core.Entities;

namespace Tandem.Service.Mapping;

public class SettlementProfile : Profile
{
    public SettlementProfile()
    {
        // The names come from the FromUser / ToUser navigations, which must be loaded (or set) beforehand
        CreateMap<Settlement, SettlementDto>()
            .ForMember(d => d.FromUserName, o => o.MapFrom(s => s.FromUser.FullName))
            .ForMember(d => d.ToUserName, o => o.MapFrom(s => s.ToUser.FullName));

        // EventTitle / EventDate come from Event.Title / Event.EventDate by flattening.
        // IOwe and the bank account depend on who is asking, so the service fills them
        CreateMap<Settlement, MySettlementDto>()
            .ForMember(d => d.FromUserName, o => o.MapFrom(s => s.FromUser.FullName))
            .ForMember(d => d.ToUserName, o => o.MapFrom(s => s.ToUser.FullName))
            .ForMember(d => d.IOwe, o => o.Ignore())
            .ForMember(d => d.RecipientBankAccount, o => o.Ignore());
    }
}
