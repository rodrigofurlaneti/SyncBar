using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.CustomerAddresses.GetByCustomerId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.CustomerAddresses.GetByCustomerId;

public sealed class GetCustomerAddressesByCustomerIdQueryHandlerTests
{
    private readonly ICustomerAddressRepository _customerAddressRepository = Substitute.For<ICustomerAddressRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetCustomerAddressesByCustomerIdQueryHandler _handler;

    public GetCustomerAddressesByCustomerIdQueryHandlerTests()
    {
        _handler = new GetCustomerAddressesByCustomerIdQueryHandler(_customerAddressRepository, _logRepository, _unitOfWork);
    }

    private static CustomerAddress CreateAddress(long companyId = 1, long? branchId = 2, long? customerId = 3)
        => CustomerAddress.Create(companyId, branchId, customerId, "Rua A", "10", "Apto 1", "00000-000").Value;

    [Fact]
    public async Task Handle_InvalidCustomerId_ShouldReturnFailure()
    {
        var query = new GetCustomerAddressesByCustomerIdQuery(0);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAddress.InvalidCustomerId");
        await _customerAddressRepository.DidNotReceive().GetByCustomerIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoAddressesForCustomer_ShouldReturnEmptyCollection()
    {
        var query = new GetCustomerAddressesByCustomerIdQuery(3);
        _customerAddressRepository.GetByCustomerIdAsync(3, Arg.Any<CancellationToken>()).Returns(Array.Empty<CustomerAddress>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AddressesFound_ShouldReturnOnlyActiveOnesMapped()
    {
        var activeAddress = CreateAddress(customerId: 3);
        var inactiveAddress = CreateAddress(customerId: 3);
        inactiveAddress.Deactivate();
        var query = new GetCustomerAddressesByCustomerIdQuery(3);
        _customerAddressRepository.GetByCustomerIdAsync(3, Arg.Any<CancellationToken>())
            .Returns(new List<CustomerAddress> { activeAddress, inactiveAddress });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Should().ContainSingle().Subject;
        response.Id.Should().Be(activeAddress.Id);
        response.CustomerId.Should().Be(activeAddress.CustomerId);
        response.Street.Should().Be(activeAddress.Street);
    }
}
