using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Comandas;
using SyncBar.Application.Features.Comandas.GetByBranch;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Comandas.GetByBranch;

public sealed class GetComandasByBranchQueryHandlerTests
{
    private readonly IComandaRepository _comandaRepository = Substitute.For<IComandaRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetComandasByBranchQueryHandler _handler;

    public GetComandasByBranchQueryHandlerTests()
    {
        _handler = new GetComandasByBranchQueryHandler(_comandaRepository, _logRepository, _unitOfWork);
    }

    private static Comanda CreateComanda(long branchId = 1, long comandaStatusId = 1, string code = "001")
        => Comanda.Create(branchId, comandaStatusId, code).Value;

    [Fact]
    public async Task Handle_NoComandasForBranch_ShouldReturnEmptyCollection()
    {
        var query = new GetComandasByBranchQuery(BranchId: 1);
        _comandaRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Comanda>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        // Query handler não faz commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleComandas_ShouldOrderByCodeLengthThenByCodeAndMapAllFields()
    {
        var query = new GetComandasByBranchQuery(BranchId: 1);
        // Comprimentos diferentes de propósito: "9" (1 char) deve vir antes de "003"/"010" (3 chars),
        // mesmo sendo "menor" numericamente que 10 mas "maior" que 3 como texto —
        // o handler ordena por Code.Length primeiro, depois por Code (ordinal).
        var comandaShort = CreateComanda(code: "9");
        var comandaLongA = CreateComanda(code: "010");
        var comandaLongB = CreateComanda(code: "003");

        _comandaRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns([comandaLongA, comandaShort, comandaLongB]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Select(r => r.Code).Should().ContainInOrder("9", "003", "010");

        var firstResponse = result.Value.First();
        firstResponse.Id.Should().Be(comandaShort.Id);
        firstResponse.BranchId.Should().Be(comandaShort.BranchId);
        firstResponse.ComandaStatusId.Should().Be(comandaShort.ComandaStatusId);
        firstResponse.Code.Should().Be(comandaShort.Code);
    }
}
