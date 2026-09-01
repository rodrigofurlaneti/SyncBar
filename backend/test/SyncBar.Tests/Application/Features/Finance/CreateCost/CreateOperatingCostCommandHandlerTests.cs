using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Finance.CreateCost;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Finance.CreateCost;

public sealed class CreateOperatingCostCommandHandlerTests
{
    private readonly IOperatingCostRepository _costRepository = Substitute.For<IOperatingCostRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateOperatingCostCommandHandler _handler;

    public CreateOperatingCostCommandHandlerTests()
    {
        _handler = new CreateOperatingCostCommandHandler(_costRepository, _logRepository, _unitOfWork);
    }

    private static CreateOperatingCostCommand CreateCommand(
        string description = "  Aluguel  ",
        decimal amount = 1500m,
        long costTypeId = CostTypeIds.Fixo,
        int referenceYear = 2026,
        int referenceMonth = 9)
        => new(BranchId: 1, CostTypeId: costTypeId, Description: description, Amount: amount,
            ReferenceYear: referenceYear, ReferenceMonth: referenceMonth);

    [Fact]
    public async Task Handle_EmptyDescription_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateCommand(description: "   ");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OperatingCost.EmptyDescription");
        await _costRepository.DidNotReceive().AddAsync(Arg.Any<OperatingCost>(), Arg.Any<CancellationToken>());
        // Nenhum commit explicito do handler nesse caminho: so o do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidAmount_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateCommand(amount: 0m);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OperatingCost.InvalidAmount");
        await _costRepository.DidNotReceive().AddAsync(Arg.Any<OperatingCost>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldTrimDescriptionPersistCostAndReturnItsId()
    {
        var command = CreateCommand(description: "  Aluguel  ", amount: 1500m, costTypeId: CostTypeIds.Fixo,
            referenceYear: 2026, referenceMonth: 9);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _costRepository.Received(1).AddAsync(
            Arg.Is<OperatingCost>(c =>
                c.BranchId == command.BranchId &&
                c.CostTypeId == command.CostTypeId &&
                c.Description == "Aluguel" &&
                c.Amount == command.Amount &&
                c.ReferenceYear == command.ReferenceYear &&
                c.ReferenceMonth == command.ReferenceMonth &&
                c.IsActive),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
