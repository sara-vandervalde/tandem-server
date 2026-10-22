using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tandem.Core.Common;
using Tandem.Core.Entities;
using Tandem.Core.Interfaces.Repositories;
using Tandem.Service.Services;

namespace Tandem.Tests.Services;

// Leaving a group, or being removed from it
public class GroupServiceTests
{
    private const int GroupId = 1;
    private const int Dana = 100;   // the manager
    private const int Noa = 200;
    private const int Rina = 300;

    private readonly Mock<IGroupRepository> _groupRepository = new();
    private readonly GroupService _service;

    public GroupServiceTests()
    {
        _service = new GroupService(_groupRepository.Object, Mock.Of<IMapper>(), NullLogger<GroupService>.Instance);
    }

    private Group GivenGroup(params int[] memberIds)
    {
        var group = new Group { Id = GroupId, ManagerId = Dana };
        foreach (var id in memberIds)
        {
            group.Members.Add(new GroupMember { GroupId = GroupId, UserId = id });
        }
        _groupRepository
            .Setup(r => r.GetWithMembersForUpdateAsync(GroupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        return group;
    }

    [Fact]
    public async Task MemberLeaves_IsRemovedAndSaved()
    {
        var group = GivenGroup(Dana, Noa);

        var result = await _service.RemoveMemberAsync(GroupId, Noa, Noa, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(group.Members, m => m.UserId == Noa);
        _groupRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ManagerLeavesWithoutHandingOver_ReturnsConflict()
    {
        var group = GivenGroup(Dana, Noa);

        var result = await _service.RemoveMemberAsync(GroupId, Dana, Dana, CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains(group.Members, m => m.UserId == Dana);
        _groupRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MemberWithOpenResponsibility_ReturnsConflict()
    {
        // Noa is responsible for something in an event that is not settled yet
        var group = GivenGroup(Dana, Noa);
        _groupRepository
            .Setup(r => r.HasOpenResponsibilitiesAsync(GroupId, Noa, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.RemoveMemberAsync(GroupId, Noa, Noa, CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains(group.Members, m => m.UserId == Noa);
    }

    [Fact]
    public async Task RegularMemberRemovesAnother_ReturnsForbidden()
    {
        GivenGroup(Dana, Noa, Rina);

        var result = await _service.RemoveMemberAsync(GroupId, Rina, Noa, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task ManagerRemovesMember_Succeeds()
    {
        var group = GivenGroup(Dana, Noa);

        var result = await _service.RemoveMemberAsync(GroupId, Noa, Dana, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(group.Members, m => m.UserId == Noa);
    }
}
