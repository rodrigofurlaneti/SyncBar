using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.CustomerAddresses.GetByBranchId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.CustomerAddresses.GetByBranchId;

public sealed class GetCustomerAddressesByBranchIdQueryHandlerTests
{
    private readonly ICustomerAddressRepository _customerAddressRepository = Substitute.For<ICustomerAddressRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetCustomerAddressesByBranchIdQueryHandler _handler;

    public GetCustomerAddressesByBranchIdQueryHandlerTests()
    {
        _handler = new GetCustomerAddressesByBranchIdQueryHandler(_customerAddressRepository, _logRepository, _unitOfWork);
    }

    private static CustomerAddress CreateAddress(long companyId = 1, long? branchId = 2, long? customerId = 3)
        => CustomerAddress.Create(companyId, branchId, customerId, "Rua A", "10", "Apto 1", "00000-000").Value;

    [Fact]
    public async Task Handle_InvalidBranchId_ShouldReturnFailure()
    {
        var query = new GetCustomerAddressesByBranchIdQuery(0);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAddress.InvalidBranchId");
        await _customerAddressRepository.DidNotReceive().GetByBranchIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoAddressesForBranch_ShouldReturnEmptyCollection()
    {
        var query = new GetCustomerAddressesByBranchIdQuery(1);
        _customerAddressRepository.GetByBranchIdAsync(1, Arg.Any<CancellationToken>()).Returns(Array.Empty<CustomerAddress>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AddressesFound_ShouldReturnOnlyActiveOnesMapped()
    {
        var activeAddress = CreateAddress(branchId: 1);
        var inactiveAddress = CreateAddress(branchId: 1);
        inactiveAddress.Deactivate();
        var query = new GetCustomerAddressesByBranchIdQuery(1);
        _customerAddressRepository.GetByBranchIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<CustomerAddress> { activeAddress, inactiveAddress });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Should().ContainSingle().Subject;
        response.Id.Should().Be(activeAddress.Id);
        response.BranchId.Should().Be(activeAddress.BranchId);
        response.Street.Should().Be(activeAddress.Street);
    }
}
