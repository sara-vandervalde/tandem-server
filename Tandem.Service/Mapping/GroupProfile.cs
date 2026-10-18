using AutoMapper;
using Tandem.Core.DTOs.Groups;
using Tandem.Core.Entities;

namespace Tandem.Service.Mapping;

public class GroupProfile : Profile
{
    public GroupProfile()
    {
        // Fields whose names differ from the entity are mapped explicitly; the rest are copied by name
        CreateMap<Group, GroupDto>()
            .ForMember(d => d.ManagerName, o => o.MapFrom(s => s.Manager.FullName))
            .ForMember(d => d.MemberCount, o => o.MapFrom(s => s.Members.Count));

        CreateMap<Group, GroupDetailsDto>()
            .ForMember(d => d.ManagerName, o => o.MapFrom(s => s.Manager.FullName))
            .ForMember(d => d.Members, o => o.MapFrom(s => s.Members.OrderBy(m => m.User.FullName)));

        CreateMap<GroupMember, GroupMemberDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.User.FullName))
            .ForMember(d => d.Email, o => o.MapFrom(s => s.User.Email));
    }
}
