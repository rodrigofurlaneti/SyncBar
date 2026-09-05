using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetByAsaasCustomerId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Customer.GetByAsaasCustomerId;

public sealed class GetByAsaasCustomerIdQueryHandlerTests
{
    private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository = Substitute.For<IAsaasIntegrationCustomerRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetByAsaasCustomerIdQueryHandler _handler;

    public GetByAsaasCustomerIdQueryHandlerTests()
    {
        _handler = new GetByAsaasCustomerIdQueryHandler(_asaasCustomerRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_BindingNotFound_ShouldReturnNotFound()
    {
        _asaasCustomerRepository.GetByAsaasCustomerIdAsync("cus_123", Arg.Any<CancellationToken>())
            .Returns((AsaasIntegrationCustomer?)null);

        var result = await _handler.Handle(new GetByAsaasCustomerIdQuery("cus_123"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasCustomer.NotFound");
    }

    [Fact]
    public async Task Handle_BindingFound_ShouldReturnMappedResponse()
    {
        var customer = AsaasIntegrationCustomer.Create(1, 1, "cus_123").Value;
        _asaasCustomerRepository.GetByAsaasCustomerIdAsync("cus_123", Arg.Any<CancellationToken>()).Returns(customer);

        var result = await _handler.Handle(new GetByAsaasCustomerIdQuery("cus_123"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerId.Should().Be(1);
    }
}
