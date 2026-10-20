using AutoMapper;
using Tandem.Core.DTOs.Items;
using Tandem.Core.Entities;

namespace Tandem.Service.Mapping;

public class ItemProfile : Profile
{
    public ItemProfile()
    {
        CreateMap<EventItem, ItemDto>()
            .ForMember(d => d.AddedByName, o => o.MapFrom(s => s.AddedBy.FullName))
            .ForMember(d => d.AssigneeName, o => o.MapFrom(s => s.Assignee != null ? s.Assignee.FullName : null));
    }
}
