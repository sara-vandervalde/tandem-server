using AutoMapper;
using Tandem.Core.DTOs.Users;
using Tandem.Core.Entities;

namespace Tandem.Service.Mapping;

public class UserProfile : Profile
{
    public UserProfile()
    {
        // Same property names are copied automatically; the Role enum becomes its name ("Admin" / "Member").
        // PasswordHash has no matching property in UserDto, so it is never exposed
        CreateMap<User, UserDto>();
    }
}
