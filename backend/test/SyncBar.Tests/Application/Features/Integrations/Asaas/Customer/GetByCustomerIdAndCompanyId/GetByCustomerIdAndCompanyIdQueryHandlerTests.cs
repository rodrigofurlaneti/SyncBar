using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetByCustomerIdAndCompanyId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Customer.GetByCustomerIdAndCompanyId;

public sealed class GetByCustomerIdAndCompanyIdQueryHandlerTests
{
    private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository = Substitute.For<IAsaasIntegrationCustomerRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetByCustomerIdAndCompanyIdQueryHandler _handler;

    public GetByCustomerIdAndCompanyIdQueryHandlerTests()
    {
        _handler = new GetByCustomerIdAndCompanyIdQueryHandler(_asaasCustomerRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_BindingNotFound_ShouldReturnNotFound()
    {
        _asaasCustomerRepository.GetByCustomerIdAndCompanyIdAsync(1, 1, Arg.Any<CancellationToken>())
            .Returns((AsaasIntegrationCustomer?)null);

        var result = await _handler.Handle(new GetByCustomerIdAndCompanyIdQuery(1, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasCustomer.NotFound");
    }

    [Fact]
    public async Task Handle_BindingFound_ShouldReturnMappedResponseIncludingUpdatedAt()
    {
        var customer = AsaasIntegrationCustomer.Create(1, 1, "cus_old").Value;
        customer.UpdateAsaasCustomerId("cus_new");
        _asaasCustomerRepository.GetByCustomerIdAndCompanyIdAsync(1, 1, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await _handler.Handle(new GetByCustomerIdAndCompanyIdQuery(1, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AsaasCustomerId.Should().Be("cus_new");
        result.Value.UpdatedAt.Should().Be(customer.UpdatedAt);
        result.Value.UpdatedAt.Should().NotBeNull();
    }
}
