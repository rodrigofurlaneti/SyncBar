using System.Reflection;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Integrations.Asaas;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Asaas;

// AsaasCustomerProvisioningService (mesmo namespace Infrastructure.Integrations.Asaas) resolve o
// parâmetro `IAsaasService` sem qualificação pra IAsaasService DESTE namespace (idêntico em forma
// à interface pública de Application, mas um tipo diferente) — mesmo-namespace vence sobre using.
public sealed class AsaasCustomerProvisioningServiceTests
{
    private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository = Substitute.For<IAsaasIntegrationCustomerRepository>();
    private readonly IAsaasService _asaasService = Substitute.For<IAsaasService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AsaasCustomerProvisioningService _service;

    public AsaasCustomerProvisioningServiceTests()
    {
        _service = new AsaasCustomerProvisioningService(_asaasCustomerRepository, _asaasService, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static Customer CreateCustomer(string? cpf = "12345678900", string? email = "cliente@example.com")
    {
        var customer = Customer.Create(1, "Cliente Teste", "11999999999", cpf, email).Value;
        SetId(customer, 7);
        return customer;
    }

    [Fact]
    public async Task EnsureLinkedAsync_ExistingBinding_ShouldReturnItWithoutCallingAsaas()
    {
        var customer = CreateCustomer();
        var binding = AsaasIntegrationCustomer.Create(customer.Id, 1, "cus_existing").Value;
        _asaasCustomerRepository.GetByCustomerIdAndCompanyIdAsync(customer.Id, 1, Arg.Any<CancellationToken>()).Returns(binding);

        var result = await _service.EnsureLinkedAsync(customer, 1, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("cus_existing");
        await _asaasService.DidNotReceive().ConfigureForTenantAsync(Arg.Any<long>(), Arg.Any<long?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureLinkedAsync_MissingCpf_ShouldReturnValidationFailure()
    {
        var customer = CreateCustomer(cpf: null);
        _asaasCustomerRepository.GetByCustomerIdAndCompanyIdAsync(customer.Id, 1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationCustomer?)null);

        var result = await _service.EnsureLinkedAsync(customer, 1, null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customer.IncompleteProfile");
        await _asaasService.DidNotReceive().ConfigureForTenantAsync(Arg.Any<long>(), Arg.Any<long?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureLinkedAsync_MissingEmail_ShouldReturnValidationFailure()
    {
        var customer = CreateCustomer(email: null);
        _asaasCustomerRepository.GetByCustomerIdAndCompanyIdAsync(customer.Id, 1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationCustomer?)null);

        var result = await _service.EnsureLinkedAsync(customer, 1, null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Customer.IncompleteProfile");
    }

    [Fact]
    public async Task EnsureLinkedAsync_AsaasApiThrows_ShouldReturnFailureWithMessage()
    {
        var customer = CreateCustomer();
        _asaasCustomerRepository.GetByCustomerIdAndCompanyIdAsync(customer.Id, 1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationCustomer?)null);
        _asaasService.CreateCustomerAsync(customer.Name, customer.Cpf!, customer.Email!, customer.Phone, Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Asaas fora do ar"));

        var result = await _service.EnsureLinkedAsync(customer, 1, 2, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AsaasApi.CreateCustomerFailed");
        result.Error.Message.Should().Contain("Asaas fora do ar");
        await _asaasService.Received(1).ConfigureForTenantAsync(1, 2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureLinkedAsync_NewCustomer_ShouldCreateBindingAndCommit()
    {
        var customer = CreateCustomer();
        _asaasCustomerRepository.GetByCustomerIdAndCompanyIdAsync(customer.Id, 1, Arg.Any<CancellationToken>()).Returns((AsaasIntegrationCustomer?)null);
        _asaasService.CreateCustomerAsync(customer.Name, customer.Cpf!, customer.Email!, customer.Phone, Arg.Any<CancellationToken>())
            .Returns("cus_new");

        var result = await _service.EnsureLinkedAsync(customer, 1, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("cus_new");
        await _asaasCustomerRepository.Received(1).AddAsync(
            Arg.Is<AsaasIntegrationCustomer>(b => b.CustomerId == customer.Id && b.CompanyId == 1 && b.AsaasCustomerId == "cus_new"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
