using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.CustomerAddresses.Remove;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.CustomerAddresses.Remove;

public sealed class RemoveCustomerAddressCommandHandlerTests
{
    private readonly ICustomerAddressRepository _customerAddressRepository = Substitute.For<ICustomerAddressRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RemoveCustomerAddressCommandHandler _handler;

    public RemoveCustomerAddressCommandHandlerTests()
    {
        _handler = new RemoveCustomerAddressCommandHandler(_customerAddressRepository, _logRepository, _unitOfWork);
    }

    private static CustomerAddress CreateActiveAddress() =>
        CustomerAddress.Create(1, 2, 3, "Main Street", "123", "Apt 1", "12345-678").Value;

    [Fact]
    public async Task Handle_AddressNotFound_ShouldReturnFailure()
    {
        var command = new RemoveCustomerAddressCommand(99);
        _customerAddressRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns((CustomerAddress?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAddress.NotFound");
        await _customerAddressRepository.DidNotReceive().RemoveAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        // O handler retorna antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AddressInactive_ShouldReturnFailure()
    {
        var address = CreateActiveAddress();
        address.Deactivate();
        var command = new RemoveCustomerAddressCommand(address.Id);
        _customerAddressRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(address);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAddress.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldRemoveAndCommitTwice()
    {
        var address = CreateActiveAddress();
        var command = new RemoveCustomerAddressCommand(address.Id);
        _customerAddressRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>())
            .Returns(address);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _customerAddressRepository.Received(1).RemoveAsync(command.Id, Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
