using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.CustomerAddresses.GetByCompanyId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.CustomerAddresses.GetByCompanyId;

public sealed class GetCustomerAddressesByCompanyIdQueryHandlerTests
{
    private readonly ICustomerAddressRepository _customerAddressRepository = Substitute.For<ICustomerAddressRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetCustomerAddressesByCompanyIdQueryHandler _handler;

    public GetCustomerAddressesByCompanyIdQueryHandlerTests()
    {
        _handler = new GetCustomerAddressesByCompanyIdQueryHandler(_customerAddressRepository, _logRepository, _unitOfWork);
    }

    private static CustomerAddress CreateAddress(long companyId = 1, long? branchId = 2, long? customerId = 3)
        => CustomerAddress.Create(companyId, branchId, customerId, "Rua A", "10", "Apto 1", "00000-000").Value;

    [Fact]
    public async Task Handle_InvalidCompanyId_ShouldReturnFailure()
    {
        var query = new GetCustomerAddressesByCompanyIdQuery(0);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAddress.InvalidCompanyId");
        await _customerAddressRepository.DidNotReceive().GetByCompanyIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoAddressesForCompany_ShouldReturnEmptyCollection()
    {
        var query = new GetCustomerAddressesByCompanyIdQuery(1);
        _customerAddressRepository.GetByCompanyIdAsync(1, Arg.Any<CancellationToken>()).Returns(Array.Empty<CustomerAddress>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AddressesFound_ShouldReturnOnlyActiveOnesMapped()
    {
        var activeAddress = CreateAddress(companyId: 1);
        var inactiveAddress = CreateAddress(companyId: 1);
        inactiveAddress.Deactivate();
        var query = new GetCustomerAddressesByCompanyIdQuery(1);
        _customerAddressRepository.GetByCompanyIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<CustomerAddress> { activeAddress, inactiveAddress });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Should().ContainSingle().Subject;
        response.Id.Should().Be(activeAddress.Id);
        response.CompanyId.Should().Be(activeAddress.CompanyId);
        response.Street.Should().Be(activeAddress.Street);
    }
}
