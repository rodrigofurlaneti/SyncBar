using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId;

public sealed class GetAllAsaasCustomersByCompanyIdQueryHandlerTests
{
    private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository = Substitute.For<IAsaasIntegrationCustomerRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAllAsaasCustomersByCompanyIdQueryHandler _handler;

    public GetAllAsaasCustomersByCompanyIdQueryHandlerTests()
    {
        _handler = new GetAllAsaasCustomersByCompanyIdQueryHandler(_asaasCustomerRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoBindings_ShouldReturnEmptyList()
    {
        _asaasCustomerRepository.GetAllByCompanyIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<AsaasIntegrationCustomer>());

        var result = await _handler.Handle(new GetAllAsaasCustomersByCompanyIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_HasBindings_ShouldReturnMappedList()
    {
        var bindings = new List<AsaasIntegrationCustomer>
        {
            AsaasIntegrationCustomer.Create(1, 1, "cus_1").Value,
            AsaasIntegrationCustomer.Create(2, 1, "cus_2").Value,
        };
        _asaasCustomerRepository.GetAllByCompanyIdAsync(1, Arg.Any<CancellationToken>()).Returns(bindings);

        var result = await _handler.Handle(new GetAllAsaasCustomersByCompanyIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(c => c.AsaasCustomerId == "cus_1");
        result.Value.Should().Contain(c => c.AsaasCustomerId == "cus_2");
    }
}
