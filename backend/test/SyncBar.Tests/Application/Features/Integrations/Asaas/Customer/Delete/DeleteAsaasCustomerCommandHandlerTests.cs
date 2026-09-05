using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Asaas;
using SyncBar.Application.Features.Integrations.Asaas.Customer.Delete;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Customer.Delete;

public sealed class DeleteAsaasCustomerCommandHandlerTests
{
    private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository = Substitute.For<IAsaasIntegrationCustomerRepository>();
    private readonly IAsaasService _asaasService = Substitute.For<IAsaasService>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeleteAsaasCustomerCommandHandler _handler;

    public DeleteAsaasCustomerCommandHandlerTests()
    {
        _handler = new DeleteAsaasCustomerCommandHandler(_asaasCustomerRepository, _asaasService, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_BindingNotFound_ShouldReturnNotFound()
    {
        var command = new DeleteAsaasCustomerCommand(1, 1);
        _asaasCustomerRepository.GetByCustomerIdAndCompanyIdForUpdateAsync(1, 1, Arg.Any<CancellationToken>())
            .Returns((AsaasIntegrationCustomer?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasCustomer.NotFound");
        await _asaasService.DidNotReceive().DeleteCustomerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AsaasApiThrows_ShouldReturnFailureWithoutDeletingLocally()
    {
        var customer = AsaasIntegrationCustomer.Create(1, 1, "cus_123").Value;
        var command = new DeleteAsaasCustomerCommand(1, 1);
        _asaasCustomerRepository.GetByCustomerIdAndCompanyIdForUpdateAsync(1, 1, Arg.Any<CancellationToken>()).Returns(customer);
        _asaasService.DeleteCustomerAsync("cus_123", Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new HttpRequestException("Asaas unavailable"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasApi.DeleteCustomerFailed");
        _asaasCustomerRepository.DidNotReceive().Delete(Arg.Any<AsaasIntegrationCustomer>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDeleteRemotelyAndLocally()
    {
        var customer = AsaasIntegrationCustomer.Create(1, 1, "cus_123").Value;
        var command = new DeleteAsaasCustomerCommand(1, 1);
        _asaasCustomerRepository.GetByCustomerIdAndCompanyIdForUpdateAsync(1, 1, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _asaasService.Received(1).DeleteCustomerAsync("cus_123", Arg.Any<CancellationToken>());
        _asaasCustomerRepository.Received(1).Delete(customer);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
