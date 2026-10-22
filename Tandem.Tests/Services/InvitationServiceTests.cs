using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tandem.Core.Common;
using Tandem.Core.DTOs.Invitations;
using Tandem.Core.Entities;
using Tandem.Core.Enums;
using Tandem.Core.Interfaces;
using Tandem.Core.Interfaces.Repositories;
using Tandem.Service.Authorization;
using Tandem.Service.Services;

namespace Tandem.Tests.Services;

// Answering an invitation: accepting adds the user to the group in the same save
public class InvitationServiceTests
{
    private const int InvitationId = 5;
    private const int GroupId = 1;
    private const int Noa = 200;
    private const string NoaEmail = "noa@test.com";

    private readonly Mock<IInvitationRepository> _invitationRepository = new();
    private readonly Mock<IGroupRepository> _groupRepository = new();
    private readonly Mock<INotificationSender> _notificationSender = new();
    private readonly InvitationService _service;

    public InvitationServiceTests()
    {
        _service = new InvitationService(
            _invitationRepository.Object,
            _groupRepository.Object,
            new GroupAccessService(_groupRepository.Object),
            _notificationSender.Object,
            Mock.Of<IMapper>(),
            NullLogger<InvitationService>.Instance);
    }

    // A pending invitation for Noa's email, valid for another week
    private GroupInvitation GivenInvitation()
    {
        var invitation = new GroupInvitation
        {
            Id = InvitationId,
            GroupId = GroupId,
            Group = new Group { Id = GroupId, Name = "Family", Manager = new User { Email = "dana@test.com" } },
            Email = NoaEmail,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _invitationRepository
            .Setup(r => r.GetByIdForUpdateAsync(InvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        return invitation;
    }

    [Fact]
    public async Task Accept_AddsMemberAndClosesInvitation()
    {
        var invitation = GivenInvitation();

        var result = await _service.RespondAsync(InvitationId, true, Noa, NoaEmail, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        _groupRepository.Verify(r => r.AddMember(
            It.Is<GroupMember>(m => m.GroupId == GroupId && m.UserId == Noa)), Times.Once);
        _invitationRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Decline_DoesNotAddMember()
    {
        var invitation = GivenInvitation();

        var result = await _service.RespondAsync(InvitationId, false, Noa, NoaEmail, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(InvitationStatus.Declined, invitation.Status);
        _groupRepository.Verify(r => r.AddMember(It.IsAny<GroupMember>()), Times.Never);
    }

    [Fact]
    public async Task Expired_ReturnsConflictWithoutSaving()
    {
        var invitation = GivenInvitation();
        invitation.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);

        var result = await _service.RespondAsync(InvitationId, true, Noa, NoaEmail, CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        _invitationRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(InvitationStatus.Accepted)]
    [InlineData(InvitationStatus.Declined)]
    [InlineData(InvitationStatus.Cancelled)]
    public async Task AlreadyHandled_ReturnsConflict(InvitationStatus status)
    {
        var invitation = GivenInvitation();
        invitation.Status = status;

        var result = await _service.RespondAsync(InvitationId, true, Noa, NoaEmail, CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        _groupRepository.Verify(r => r.AddMember(It.IsAny<GroupMember>()), Times.Never);
    }

    [Fact]
    public async Task RegularMemberInvites_ReturnsForbidden()
    {
        // Only the manager (Dana, 100) invites; Noa is a regular member
        _groupRepository
            .Setup(r => r.GetByIdAsync(GroupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Group { Id = GroupId, ManagerId = 100 });

        var result = await _service.CreateAsync(
            GroupId, new CreateInvitationsRequest { Emails = ["rina@test.com"] }, Noa, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _invitationRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvitationOfAnotherEmail_ReturnsForbidden()
    {
        GivenInvitation();

        var result = await _service.RespondAsync(InvitationId, true, 300, "someone.else@test.com", CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _groupRepository.Verify(r => r.AddMember(It.IsAny<GroupMember>()), Times.Never);
    }
}
