using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tandem.Core.Common;
using Tandem.Core.DTOs.Events;
using Tandem.Core.Entities;
using Tandem.Core.Enums;
using Tandem.Core.Exceptions;
using Tandem.Core.Interfaces.Repositories;
using Tandem.Service.Authorization;
using Tandem.Service.Services;

namespace Tandem.Tests.Services;

// Claiming a responsibility area - the limited resource of the system.
// The repositories are mocks: no database, only the service's own logic is tested
public class EventServiceClaimTests
{
    private const int EventId = 10;
    private const int GroupId = 1;
    private const int Dana = 100;
    private const int Noa = 200;

    private readonly Mock<IEventRepository> _eventRepository = new();
    private readonly Mock<IGroupRepository> _groupRepository = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly EventService _service;

    public EventServiceClaimTests()
    {
        _service = new EventService(
            _eventRepository.Object,
            _groupRepository.Object,
            new GroupAccessService(_groupRepository.Object),
            _mapper.Object,
            NullLogger<EventService>.Instance);

        // Dana and Noa are members of the group
        _groupRepository
            .Setup(r => r.IsMemberAsync(GroupId, It.IsIn(Dana, Noa), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mapper
            .Setup(m => m.Map<EventDetailsDto>(It.IsAny<Event>()))
            .Returns(new EventDetailsDto());
    }

    // An event organized by Dana, Open mode: any member may claim an area
    private Event GivenEvent(OrganizationMode mode = OrganizationMode.Open)
    {
        var evt = new Event
        {
            Id = EventId,
            GroupId = GroupId,
            OrganizerId = Dana,
            OrganizationMode = mode,
            Status = EventStatus.Organizing,
            Venue = new Venue(),
            Meal = new Meal()
        };
        _eventRepository
            .Setup(r => r.GetForUpdateAsync(EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        _eventRepository
            .Setup(r => r.GetDetailsAsync(EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        return evt;
    }

    [Fact]
    public async Task Claim_FreeArea_SavesAndSetsResponsible()
    {
        var evt = GivenEvent();

        var result = await _service.ClaimResponsibilityAsync(EventId, ResponsibilityArea.Venue, Noa, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Noa, evt.Venue.ResponsibleId);
        _eventRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Claim_AreaTakenBySomeoneElse_ReturnsConflictWithoutSaving()
    {
        var evt = GivenEvent();
        evt.Venue.ResponsibleId = Dana;

        var result = await _service.ClaimResponsibilityAsync(EventId, ResponsibilityArea.Venue, Noa, CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal(Dana, evt.Venue.ResponsibleId);
        _eventRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Claim_LostTheRace_ReturnsConflict()
    {
        // Both passed the "is it free?" check; the other member saved first, so our save finds a different version
        GivenEvent();
        _eventRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException(new Exception("row version changed")));

        var result = await _service.ClaimResponsibilityAsync(EventId, ResponsibilityArea.Venue, Noa, CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task Claim_InSoloByMemberWhoIsNotOrganizer_ReturnsForbidden()
    {
        GivenEvent(OrganizationMode.Solo);

        var result = await _service.ClaimResponsibilityAsync(EventId, ResponsibilityArea.Venue, Noa, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Claim_InSoloByOrganizer_Succeeds()
    {
        var evt = GivenEvent(OrganizationMode.Solo);

        var result = await _service.ClaimResponsibilityAsync(EventId, ResponsibilityArea.Venue, Dana, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Dana, evt.Venue.ResponsibleId);
    }

    [Fact]
    public async Task Claim_ByUserOutsideTheGroup_ReturnsForbidden()
    {
        GivenEvent();
        const int stranger = 999;

        var result = await _service.ClaimResponsibilityAsync(EventId, ResponsibilityArea.Venue, stranger, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }
}
