using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Asaas.Customer.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Customer.Create;

public sealed class CreateAsaasIntegrationCustomerCommandHandlerTests
{
    private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository = Substitute.For<IAsaasIntegrationCustomerRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateAsaasIntegrationCustomerCommandHandler _handler;

    public CreateAsaasIntegrationCustomerCommandHandlerTests()
    {
        _handler = new CreateAsaasIntegrationCustomerCommandHandler(_asaasCustomerRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_BindingAlreadyExists_ShouldReturnConflict()
    {
        var command = new CreateAsaasIntegrationCustomerCommand(1, 1, "cus_123");
        _asaasCustomerRepository.ExistsAsync(1, 1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasCustomer.AlreadyExists");
        await _asaasCustomerRepository.DidNotReceive().AddAsync(Arg.Any<AsaasIntegrationCustomer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyAsaasCustomerId_ShouldReturnDomainValidationFailure()
    {
        var command = new CreateAsaasIntegrationCustomerCommand(1, 1, "  ");
        _asaasCustomerRepository.ExistsAsync(1, 1, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasCustomerId.Empty");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistAndReturnNewId()
    {
        var command = new CreateAsaasIntegrationCustomerCommand(1, 1, "cus_123");
        _asaasCustomerRepository.ExistsAsync(1, 1, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _asaasCustomerRepository.Received(1).AddAsync(
            Arg.Is<AsaasIntegrationCustomer>(c => c.CustomerId == 1 && c.CompanyId == 1 && c.AsaasCustomerId == "cus_123"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
