using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tandem.Core.Common;
using Tandem.Core.DTOs.Items;
using Tandem.Core.Entities;
using Tandem.Core.Enums;
using Tandem.Core.Exceptions;
using Tandem.Core.Interfaces.Repositories;
using Tandem.Service.Authorization;
using Tandem.Service.Services;

namespace Tandem.Tests.Services;

// Claiming an item on the "what to bring" list
public class ItemServiceTests
{
    private const int ItemId = 7;
    private const int GroupId = 1;
    private const int Dana = 100;   // responsible for the meal
    private const int Noa = 200;    // a member

    private readonly Mock<IItemRepository> _itemRepository = new();
    private readonly Mock<IGroupRepository> _groupRepository = new();
    private readonly ItemService _service;

    public ItemServiceTests()
    {
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<ItemDto>(It.IsAny<EventItem>())).Returns(new ItemDto());

        _service = new ItemService(
            _itemRepository.Object,
            Mock.Of<IEventRepository>(),
            new GroupAccessService(_groupRepository.Object),
            mapper.Object,
            NullLogger<ItemService>.Instance);

        // The group exists, and Dana and Noa are its members
        _groupRepository
            .Setup(r => r.GetByIdAsync(GroupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Group { Id = GroupId });
        _groupRepository
            .Setup(r => r.IsMemberAsync(GroupId, It.IsIn(Dana, Noa), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private EventItem GivenItem(bool isPrivate)
    {
        var evt = new Event
        {
            Id = 10,
            GroupId = GroupId,
            OrganizerId = Dana,
            Status = EventStatus.Organizing,
            Venue = new Venue(),
            Meal = new Meal { ResponsibleId = Dana }
        };
        var item = new EventItem { Id = ItemId, Event = evt, Type = ItemType.Food, Name = "Cake", IsPrivate = isPrivate };
        evt.Items.Add(item);

        _itemRepository
            .Setup(r => r.GetForUpdateAsync(ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _itemRepository
            .Setup(r => r.GetDetailsAsync(ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        return item;
    }

    [Fact]
    public async Task Claim_PublicItem_Succeeds()
    {
        var item = GivenItem(isPrivate: false);

        var result = await _service.ClaimAsync(ItemId, Noa, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Noa, item.AssigneeId);
    }

    [Fact]
    public async Task Claim_PrivateItemByOtherMember_ReturnsNotFound()
    {
        // The cook's private menu does not exist for anyone else - not even "forbidden"
        var item = GivenItem(isPrivate: true);

        var result = await _service.ClaimAsync(ItemId, Noa, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Null(item.AssigneeId);
        _itemRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Claim_ItemTakenBySomeoneElse_ReturnsConflict()
    {
        var item = GivenItem(isPrivate: false);
        item.AssigneeId = Dana;

        var result = await _service.ClaimAsync(ItemId, Noa, CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal(Dana, item.AssigneeId);
    }

    [Fact]
    public async Task Claim_LostTheRace_ReturnsConflict()
    {
        GivenItem(isPrivate: false);
        _itemRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException(new Exception("row version changed")));

        var result = await _service.ClaimAsync(ItemId, Noa, CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task Claim_LastPublicItem_MakesEventOrganized()
    {
        // Everything else is ready; the cake was the only thing missing
        var item = GivenItem(isPrivate: false);
        item.Event.Venue = new Venue { ResponsibleId = Dana, Price = 0 };
        item.Event.Meal.Price = 200;

        await _service.ClaimAsync(ItemId, Noa, CancellationToken.None);

        Assert.Equal(EventStatus.Organized, item.Event.Status);
    }
}
