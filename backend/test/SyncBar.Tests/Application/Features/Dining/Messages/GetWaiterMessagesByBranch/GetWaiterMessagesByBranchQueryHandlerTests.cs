using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Dining.Messages.GetWaiterMessagesByBranch;
using SyncBar.Domain.Repositories;
using Xunit;
using WaiterMessage = SyncBar.Domain.Entities.WaiterMessage;

namespace SyncBar.Tests.Application.Features.Dining.Messages.GetWaiterMessagesByBranch;

// Este handler foge do padrão dos demais: implementa IRequestHandler diretamente (sem
// BaseQueryHandler), então não depende de ILogTrackerRepository/IUnitOfWork nem faz commit —
// por isso o setup aqui só mocka o repositório de mensagens.
public sealed class GetWaiterMessagesByBranchQueryHandlerTests
{
    private readonly IWaiterMessageRepository _messageRepository = Substitute.For<IWaiterMessageRepository>();

    private readonly GetWaiterMessagesByBranchQueryHandler _handler;

    public GetWaiterMessagesByBranchQueryHandlerTests()
    {
        _handler = new GetWaiterMessagesByBranchQueryHandler(_messageRepository);
    }

    private static WaiterMessage CreateMessage(long branchId, long diningAreaId, string message = "Mensagem")
        => WaiterMessage.Create(branchId, senderEmployeeId: 10, recipientEmployeeId: 20, diningAreaId, message).Value;

    [Fact]
    public async Task Handle_NoDiningAreaFilter_ShouldReturnAllMessagesOfTheBranch()
    {
        var query = new GetWaiterMessagesByBranchQuery(BranchId: 1, DiningAreaId: null);
        var messageAreaOne = CreateMessage(query.BranchId, diningAreaId: 1);
        var messageAreaTwo = CreateMessage(query.BranchId, diningAreaId: 2);
        _messageRepository.GetByBranchIdAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([messageAreaOne, messageAreaTwo]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithDiningAreaFilter_ShouldReturnOnlyMessagesFromThatArea()
    {
        var query = new GetWaiterMessagesByBranchQuery(BranchId: 1, DiningAreaId: 2);
        var messageAreaOne = CreateMessage(query.BranchId, diningAreaId: 1);
        var messageAreaTwo = CreateMessage(query.BranchId, diningAreaId: 2, message: "Precisa de talheres");
        _messageRepository.GetByBranchIdAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([messageAreaOne, messageAreaTwo]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var response = result.Value.Single();
        response.Id.Should().Be(messageAreaTwo.Id);
        response.BranchId.Should().Be(messageAreaTwo.BranchId);
        response.SenderEmployeeId.Should().Be(messageAreaTwo.SenderEmployeeId);
        response.RecipientEmployeeId.Should().Be(messageAreaTwo.RecipientEmployeeId);
        response.DiningAreaId.Should().Be(messageAreaTwo.DiningAreaId);
        response.Message.Should().Be(messageAreaTwo.Message);
        response.IsRead.Should().Be(messageAreaTwo.IsRead);
        response.CreatedAt.Should().Be(messageAreaTwo.CreatedAt.ToString("o"));
    }
}
