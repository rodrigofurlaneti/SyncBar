using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.CustomerAddresses.Update;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.CustomerAddresses.Update;

public sealed class UpdateCustomerAddressCommandHandlerTests
{
    private readonly ICustomerAddressRepository _customerAddressRepository = Substitute.For<ICustomerAddressRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateCustomerAddressCommandHandler _handler;

    public UpdateCustomerAddressCommandHandlerTests()
    {
        _handler = new UpdateCustomerAddressCommandHandler(_customerAddressRepository, _logRepository, _unitOfWork);
    }

    private static CustomerAddress CreateActiveAddress() =>
        CustomerAddress.Create(1, 2, 3, "Main Street", "123", "Apt 1", "12345-678").Value;

    private static UpdateCustomerAddressCommand ValidCommand(long id) => new(
        id, 1, 2, 3, "New Street", "456", "Suite 2", "98765-432");

    [Fact]
    public async Task Handle_AddressNotFound_ShouldReturnFailure()
    {
        var command = ValidCommand(99);
        _customerAddressRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns((CustomerAddress?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAddress.NotFound");
        await _customerAddressRepository.DidNotReceive().UpdateAsync(Arg.Any<CustomerAddress>(), Arg.Any<CancellationToken>());
        // O handler retorna antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AddressInactive_ShouldReturnFailure()
    {
        var address = CreateActiveAddress();
        address.Deactivate();
        var command = ValidCommand(address.Id);
        _customerAddressRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(address);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAddress.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdateDetailsFails_ShouldReturnFailureWithoutPersisting()
    {
        var address = CreateActiveAddress();
        var command = ValidCommand(address.Id) with { Street = string.Empty };
        _customerAddressRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(address);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAddress.EmptyStreet");
        await _customerAddressRepository.DidNotReceive().UpdateAsync(Arg.Any<CustomerAddress>(), Arg.Any<CancellationToken>());
        // A falha de domínio ocorre antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldUpdateDetailsAndCommitTwice()
    {
        var address = CreateActiveAddress();
        var command = ValidCommand(address.Id);
        _customerAddressRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(address);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        address.Street.Should().Be("New Street");
        address.Number.Should().Be("456");
        address.ZipCode.Should().Be("98765-432");
        await _customerAddressRepository.Received(1).UpdateAsync(
            Arg.Is<CustomerAddress>(a => a.Street == "New Street"), Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
