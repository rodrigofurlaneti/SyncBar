using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Branches.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Branches.Create;

public sealed class CreateBranchCommandHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateBranchCommandHandler _handler;

    public CreateBranchCommandHandlerTests()
    {
        _handler = new CreateBranchCommandHandler(_branchRepository, _logRepository, _unitOfWork);
    }

    private static CreateBranchCommand CreateCommand(string name = "Filial Centro") =>
        new(
            CompanyId: 1,
            Name: name,
            Cnpj: "12345678000199",
            Phone: "1122223333",
            AddressStreet: "Rua das Flores",
            AddressNumber: "100",
            AddressDistrict: "Centro",
            AddressCity: "São Paulo",
            AddressState: "SP",
            AddressZipCode: "01000000");

    [Fact]
    public async Task Handle_EmptyName_ShouldReturnFailureWithoutPersisting()
    {
        // Branch.Create falha quando Name é vazio/whitespace -> "Branch.EmptyName".
        var command = CreateCommand(name: "   ");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.EmptyName");

        await _branchRepository.DidNotReceive().AddAsync(Arg.Any<Branch>(), Arg.Any<CancellationToken>());
        // O handler retorna antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistBranchAndReturnItsId()
    {
        var command = CreateCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        await _branchRepository.Received(1).AddAsync(
            Arg.Is<Branch>(b =>
                b.CompanyId == command.CompanyId &&
                b.Name == command.Name &&
                b.Cnpj == command.Cnpj &&
                b.Phone == command.Phone &&
                b.AddressStreet == command.AddressStreet &&
                b.AddressNumber == command.AddressNumber &&
                b.AddressDistrict == command.AddressDistrict &&
                b.AddressCity == command.AddressCity &&
                b.AddressState == command.AddressState &&
                b.AddressZipCode == command.AddressZipCode),
            Arg.Any<CancellationToken>());

        // Id é sempre 0 em teste (não há API pública para setá-lo) — o handler apenas repassa
        // o Id do branch recém-criado, então o resultado deve refletir esse mesmo valor.
        result.Value.Should().Be(0);

        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
