using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Dining.Messages.Create;
using SyncBar.Domain.Repositories;
using Xunit;
using WaiterMessage = SyncBar.Domain.Entities.WaiterMessage;

namespace SyncBar.Tests.Application.Features.Dining.Messages.Create;

public sealed class CreateWaiterMessageCommandHandlerTests
{
    private readonly IWaiterMessageRepository _messageRepository = Substitute.For<IWaiterMessageRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateWaiterMessageCommandHandler _handler;

    public CreateWaiterMessageCommandHandlerTests()
    {
        _handler = new CreateWaiterMessageCommandHandler(_messageRepository, _logRepository, _unitOfWork);
    }

    private static CreateWaiterMessageCommand CreateCommand(
        long branchId = 1, long senderEmployeeId = 10, long? recipientEmployeeId = 20, long diningAreaId = 1, string message = "Mesa 5 precisa de atendimento")
        => new(branchId, senderEmployeeId, recipientEmployeeId, diningAreaId, message);

    [Fact]
    public async Task Handle_InvalidBranchId_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateCommand(branchId: 0);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WaiterMessage.InvalidBranchId");
        await _messageRepository.DidNotReceive().AddAsync(Arg.Any<WaiterMessage>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidSenderEmployeeId_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateCommand(senderEmployeeId: 0);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WaiterMessage.InvalidSenderId");
        await _messageRepository.DidNotReceive().AddAsync(Arg.Any<WaiterMessage>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidDiningAreaId_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateCommand(diningAreaId: 0);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WaiterMessage.InvalidDiningAreaId");
        await _messageRepository.DidNotReceive().AddAsync(Arg.Any<WaiterMessage>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyMessage_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateCommand(message: "   ");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WaiterMessage.EmptyMessage");
        await _messageRepository.DidNotReceive().AddAsync(Arg.Any<WaiterMessage>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistTrimmedMessageAndReturnItsId()
    {
        var command = CreateCommand(message: "  Mesa 5 precisa de atendimento  ");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _messageRepository.Received(1).AddAsync(
            Arg.Is<WaiterMessage>(m =>
                m.BranchId == command.BranchId &&
                m.SenderEmployeeId == command.SenderEmployeeId &&
                m.RecipientEmployeeId == command.RecipientEmployeeId &&
                m.DiningAreaId == command.DiningAreaId &&
                m.Message == "Mesa 5 precisa de atendimento"),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
