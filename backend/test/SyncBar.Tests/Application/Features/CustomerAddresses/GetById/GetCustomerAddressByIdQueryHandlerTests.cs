using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.CustomerAddresses.GetById;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.CustomerAddresses.GetById;

public sealed class GetCustomerAddressByIdQueryHandlerTests
{
    private readonly ICustomerAddressRepository _customerAddressRepository = Substitute.For<ICustomerAddressRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetCustomerAddressByIdQueryHandler _handler;

    public GetCustomerAddressByIdQueryHandlerTests()
    {
        _handler = new GetCustomerAddressByIdQueryHandler(_customerAddressRepository, _logRepository, _unitOfWork);
    }

    private static CustomerAddress CreateAddress(long companyId = 1, long? branchId = 2, long? customerId = 3)
        => CustomerAddress.Create(companyId, branchId, customerId, "Rua A", "10", "Apto 1", "00000-000").Value;

    [Fact]
    public async Task Handle_InvalidId_ShouldReturnFailure()
    {
        var query = new GetCustomerAddressByIdQuery(0);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAddress.InvalidId");
        await _customerAddressRepository.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AddressNotFound_ShouldReturnFailure()
    {
        var query = new GetCustomerAddressByIdQuery(1);
        _customerAddressRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((CustomerAddress?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAddress.NotFound");
    }

    [Fact]
    public async Task Handle_AddressInactive_ShouldReturnFailure()
    {
        var address = CreateAddress();
        address.Deactivate();
        var query = new GetCustomerAddressByIdQuery(1);
        _customerAddressRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(address);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerAddress.NotFound");
    }

    [Fact]
    public async Task Handle_AddressFoundAndActive_ShouldReturnMappedResponse()
    {
        var address = CreateAddress();
        var query = new GetCustomerAddressByIdQuery(1);
        _customerAddressRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(address);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(address.Id);
        result.Value.CompanyId.Should().Be(address.CompanyId);
        result.Value.BranchId.Should().Be(address.BranchId);
        result.Value.CustomerId.Should().Be(address.CustomerId);
        result.Value.Street.Should().Be(address.Street);
        result.Value.Number.Should().Be(address.Number);
        result.Value.Supplement.Should().Be(address.Supplement);
        result.Value.ZipCode.Should().Be(address.ZipCode);
        result.Value.IsActive.Should().BeTrue();
    }
}
