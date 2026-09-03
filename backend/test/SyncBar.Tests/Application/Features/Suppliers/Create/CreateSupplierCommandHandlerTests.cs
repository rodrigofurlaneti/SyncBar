using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Suppliers.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Suppliers.Create;

public sealed class CreateSupplierCommandHandlerTests
{
    private readonly ISupplierRepository _supplierRepository = Substitute.For<ISupplierRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateSupplierCommandHandler _handler;

    public CreateSupplierCommandHandlerTests()
    {
        _handler = new CreateSupplierCommandHandler(_supplierRepository, _logRepository, _unitOfWork);
    }

    private static CreateSupplierCommand CreateValidCommand(string legalName = "Distribuidora Central Ltda")
        => new(
            CompanyId: 1,
            LegalName: legalName,
            TradeName: "Central Bebidas",
            Cnpj: "12345678000199",
            Email: "contato@central.com.br",
            Phone: "11999998888");

    [Fact]
    public async Task Handle_EmptyLegalName_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand(legalName: "");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Supplier.EmptyLegalName");
        await _supplierRepository.DidNotReceive().AddAsync(Arg.Any<Supplier>(), Arg.Any<CancellationToken>());
        // Sem persistência: só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistSupplierAndReturnItsId()
    {
        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _supplierRepository.Received(1).AddAsync(
            Arg.Is<Supplier>(s =>
                s.CompanyId == command.CompanyId &&
                s.LegalName == command.LegalName &&
                s.TradeName == command.TradeName &&
                s.Cnpj == command.Cnpj &&
                s.Email == command.Email &&
                s.Phone == command.Phone &&
                s.IsActive),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequestWithOptionalFieldsNull_ShouldPersistSupplierWithNullOptionalFields()
    {
        var command = new CreateSupplierCommand(
            CompanyId: 1,
            LegalName: "Fornecedor Simples ME",
            TradeName: null,
            Cnpj: null,
            Email: null,
            Phone: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _supplierRepository.Received(1).AddAsync(
            Arg.Is<Supplier>(s =>
                s.LegalName == command.LegalName &&
                s.TradeName == null &&
                s.Cnpj == null &&
                s.Email == null &&
                s.Phone == null),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
