using AutoMapper;
using Tandem.Core.DTOs.Messages;
using Tandem.Core.Entities;

namespace Tandem.Service.Mapping;

public class MessageProfile : Profile
{
    public MessageProfile()
    {
        CreateMap<EventMessage, MessageDto>()
            .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.FullName));
    }
}
