using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.CustomerAddresses.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.CustomerAddresses.Create;

public sealed class CreateCustomerAddressCommandHandlerTests
{
    private readonly ICustomerAddressRepository _customerAddressRepository = Substitute.For<ICustomerAddressRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateCustomerAddressCommandHandler _handler;

    public CreateCustomerAddressCommandHandlerTests()
    {
        _handler = new CreateCustomerAddressCommandHandler(_customerAddressRepository, _logRepository, _unitOfWork);
    }

    private static CreateCustomerAddressCommand ValidCommand() => new(
        1,
        2,
        3,
        "Main Street",
        "123",
        "Apt 1",
        "12345-678");

    [Fact]
    public async Task Handle_EmptyStreet_ShouldReturnFailureWithoutPersisting()
    {
        var command = ValidCommand() with { Street = string.Empty };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAddress.EmptyStreet");
        await _customerAddressRepository.DidNotReceive().AddAsync(Arg.Any<CustomerAddress>(), Arg.Any<CancellationToken>());
        // O handler retorna antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyNumber_ShouldReturnFailure()
    {
        var command = ValidCommand() with { Number = string.Empty };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAddress.EmptyNumber");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NullZipCode_ShouldReturnFailure()
    {
        var command = ValidCommand() with { ZipCode = null };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAddress.EmptyZipCode");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldPersistAndReturnCreatedId()
    {
        var command = ValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _customerAddressRepository.Received(1).AddAsync(
            Arg.Is<CustomerAddress>(a =>
                a.CompanyId == command.CompanyId &&
                a.BranchId == command.BranchId &&
                a.CustomerId == command.CustomerId &&
                a.Street == command.Street &&
                a.Number == command.Number &&
                a.ZipCode == command.ZipCode),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
