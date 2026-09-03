using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Suppliers.Deactivate;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Suppliers.Deactivate;

public sealed class DeactivateSupplierCommandHandlerTests
{
    private readonly ISupplierRepository _supplierRepository = Substitute.For<ISupplierRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeactivateSupplierCommandHandler _handler;

    public DeactivateSupplierCommandHandlerTests()
    {
        _handler = new DeactivateSupplierCommandHandler(_supplierRepository, _logRepository, _unitOfWork);
    }

    private static Supplier CreateActiveSupplier()
        => Supplier.Create(companyId: 1, legalName: "Distribuidora Central Ltda", tradeName: null, cnpj: null, email: null, phone: null).Value;

    [Fact]
    public async Task Handle_SupplierNotFound_ShouldReturnFailureWithoutCommitting()
    {
        var command = new DeactivateSupplierCommand(SupplierId: 1);
        _supplierRepository.GetByIdForUpdateAsync(command.SupplierId, Arg.Any<CancellationToken>()).Returns((Supplier?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Supplier.NotFound");
        // Nenhum commit explícito do handler; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SupplierAlreadyInactive_ShouldReturnFailureWithoutCommitting()
    {
        var supplier = CreateActiveSupplier();
        supplier.Deactivate(); // já desativado antes deste Handle
        var command = new DeactivateSupplierCommand(SupplierId: 1);
        _supplierRepository.GetByIdForUpdateAsync(command.SupplierId, Arg.Any<CancellationToken>()).Returns(supplier);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Supplier.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDeactivateSupplierAndCommit()
    {
        var supplier = CreateActiveSupplier();
        var command = new DeactivateSupplierCommand(SupplierId: 1);
        _supplierRepository.GetByIdForUpdateAsync(command.SupplierId, Arg.Any<CancellationToken>()).Returns(supplier);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        supplier.IsActive.Should().BeFalse();
        // Commit explícito do handler + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
