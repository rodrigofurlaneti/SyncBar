using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.CustomerAddresses.RegisterOrder;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.CustomerAddresses.RegisterOrder;

public sealed class RegisterCustomerAddressOrderCommandHandlerTests
{
    private readonly ICustomerAddressRepository _customerAddressRepository = Substitute.For<ICustomerAddressRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RegisterCustomerAddressOrderCommandHandler _handler;

    public RegisterCustomerAddressOrderCommandHandlerTests()
    {
        _handler = new RegisterCustomerAddressOrderCommandHandler(_customerAddressRepository, _logRepository, _unitOfWork);
    }

    private static CustomerAddress CreateActiveAddress() =>
        CustomerAddress.Create(1, 2, 3, "Main Street", "123", "Apt 1", "12345-678").Value;

    [Fact]
    public async Task Handle_AddressNotFound_ShouldReturnFailure()
    {
        var command = new RegisterCustomerAddressOrderCommand(1, 99);
        _customerAddressRepository.GetByIdAsync(command.AddressId, Arg.Any<CancellationToken>())
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
        var command = new RegisterCustomerAddressOrderCommand(1, 99);
        _customerAddressRepository.GetByIdAsync(command.AddressId, Arg.Any<CancellationToken>())
            .Returns(address);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAddress.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldRegisterOrderUsageAndCommitTwice()
    {
        var address = CreateActiveAddress();
        var command = new RegisterCustomerAddressOrderCommand(address.Id, 555);
        _customerAddressRepository.GetByIdAsync(command.AddressId, Arg.Any<CancellationToken>())
            .Returns(address);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        address.LastOrderId.Should().Be(555);
        await _customerAddressRepository.Received(1).UpdateAsync(
            Arg.Is<CustomerAddress>(a => a.LastOrderId == 555), Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
