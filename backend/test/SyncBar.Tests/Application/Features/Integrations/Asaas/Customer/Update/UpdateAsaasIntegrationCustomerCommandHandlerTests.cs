using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Customer.Update;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Customer.Update;

public sealed class UpdateAsaasIntegrationCustomerCommandHandlerTests
{
    private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository = Substitute.For<IAsaasIntegrationCustomerRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateAsaasIntegrationCustomerCommandHandler _handler;

    public UpdateAsaasIntegrationCustomerCommandHandlerTests()
    {
        _handler = new UpdateAsaasIntegrationCustomerCommandHandler(_asaasCustomerRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_BindingNotFound_ShouldReturnNotFound()
    {
        var command = new UpdateAsaasIntegrationCustomerCommand(1, "cus_new");
        _asaasCustomerRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationCustomer?)null);
        _asaasCustomerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationCustomer?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasCustomer.NotFound");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldUpdateAndCommit()
    {
        var customer = AsaasIntegrationCustomer.Create(1, 1, "cus_old").Value;
        var command = new UpdateAsaasIntegrationCustomerCommand(1, "cus_new");
        _asaasCustomerRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        customer.AsaasCustomerId.Should().Be("cus_new");
        _asaasCustomerRepository.Received(1).Update(customer);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForUpdateLookupMisses_ShouldFallBackToPlainLookup()
    {
        var customer = AsaasIntegrationCustomer.Create(1, 1, "cus_old").Value;
        var command = new UpdateAsaasIntegrationCustomerCommand(1, "cus_new");
        _asaasCustomerRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationCustomer?)null);
        _asaasCustomerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        customer.AsaasCustomerId.Should().Be("cus_new");
    }
}
