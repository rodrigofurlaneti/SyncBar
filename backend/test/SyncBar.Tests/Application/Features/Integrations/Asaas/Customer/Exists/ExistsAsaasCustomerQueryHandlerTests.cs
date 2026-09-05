using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Customer.Exists;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Customer.Exists;

public sealed class ExistsAsaasCustomerQueryHandlerTests
{
    private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository = Substitute.For<IAsaasIntegrationCustomerRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ExistsAsaasCustomerQueryHandler _handler;

    public ExistsAsaasCustomerQueryHandlerTests()
    {
        _handler = new ExistsAsaasCustomerQueryHandler(_asaasCustomerRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_BindingExists_ShouldReturnTrue()
    {
        _asaasCustomerRepository.ExistsAsync(1, 1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new ExistsAsaasCustomerQuery(1, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BindingDoesNotExist_ShouldReturnFalse()
    {
        _asaasCustomerRepository.ExistsAsync(1, 1, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new ExistsAsaasCustomerQuery(1, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }
}
