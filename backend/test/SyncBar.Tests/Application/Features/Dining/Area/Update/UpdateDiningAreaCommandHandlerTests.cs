using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Dining.Area.Update;
using SyncBar.Domain.Repositories;
using Xunit;
using DiningArea = SyncBar.Domain.Entities.DiningArea;

namespace SyncBar.Tests.Application.Features.Dining.Area.Update;

public sealed class UpdateDiningAreaCommandHandlerTests
{
    private readonly IDiningAreaRepository _diningAreaRepository = Substitute.For<IDiningAreaRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateDiningAreaCommandHandler _handler;

    public UpdateDiningAreaCommandHandlerTests()
    {
        _handler = new UpdateDiningAreaCommandHandler(_diningAreaRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_DiningAreaNotFound_ShouldReturnFailureWithoutUpdating()
    {
        var command = new UpdateDiningAreaCommand(Id: 1, Name: "Novo Nome");
        _diningAreaRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((DiningArea?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningArea.NotFound");
        await _diningAreaRepository.DidNotReceive().UpdateAsync(Arg.Any<DiningArea>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidName_ShouldUpdateDiningAreaName()
    {
        var diningArea = DiningArea.Create(branchId: 1, name: "Nome Antigo").Value;
        var command = new UpdateDiningAreaCommand(Id: 1, Name: "Nome Novo");
        _diningAreaRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(diningArea);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        diningArea.Name.Should().Be("Nome Novo");
        await _diningAreaRepository.Received(1).UpdateAsync(diningArea, Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BlankName_ShouldKeepOriginalNameButStillPersist()
    {
        // DiningArea.UpdateName ignora silenciosamente nomes em branco (não retorna Result nem falha) —
        // este teste documenta o comportamento atual do handler, que não valida isso antes de persistir.
        var diningArea = DiningArea.Create(branchId: 1, name: "Nome Original").Value;
        var command = new UpdateDiningAreaCommand(Id: 1, Name: "   ");
        _diningAreaRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(diningArea);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        diningArea.Name.Should().Be("Nome Original");
        await _diningAreaRepository.Received(1).UpdateAsync(diningArea, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
