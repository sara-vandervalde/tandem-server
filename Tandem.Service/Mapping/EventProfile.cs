using AutoMapper;
using Tandem.Core.DTOs.Events;
using Tandem.Core.Entities;

namespace Tandem.Service.Mapping;

public class EventProfile : Profile
{
    public EventProfile()
    {
        // Enums (Status, SplitMethod...) become their names; "GroupName", "CreatedByName" etc. are mapped explicitly
        CreateMap<Event, EventDetailsDto>()
            .ForMember(d => d.GroupName, o => o.MapFrom(s => s.Group.Name))
            .ForMember(d => d.CreatedByName, o => o.MapFrom(s => s.CreatedBy.FullName))
            .ForMember(d => d.OrganizerName, o => o.MapFrom(s => s.Organizer != null ? s.Organizer.FullName : null));

        CreateMap<Event, EventListItemDto>()
            .ForMember(d => d.GroupName, o => o.MapFrom(s => s.Group.Name))
            .ForMember(d => d.OrganizerName, o => o.MapFrom(s => s.Organizer != null ? s.Organizer.FullName : null));

        CreateMap<EventParticipant, ParticipantDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.User.FullName));

        CreateMap<Venue, VenueDto>()
            .ForMember(d => d.ResponsibleName, o => o.MapFrom(s => s.Responsible != null ? s.Responsible.FullName : null));

        CreateMap<Gift, GiftDto>()
            .ForMember(d => d.ResponsibleName, o => o.MapFrom(s => s.Responsible != null ? s.Responsible.FullName : null));

        CreateMap<Meal, MealDto>()
            .ForMember(d => d.ResponsibleName, o => o.MapFrom(s => s.Responsible != null ? s.Responsible.FullName : null));
    }
}
