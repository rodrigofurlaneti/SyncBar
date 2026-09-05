using SyncBar.Application.Abstractions.Integrations.Asaas;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Integrations.Asaas;

internal sealed class AsaasCustomerProvisioningService(
    IAsaasIntegrationCustomerRepository asaasCustomerRepository,
    IAsaasService asaasService,
    IUnitOfWork unitOfWork) : IAsaasCustomerProvisioningService
{
    public async Task<Result<string>> EnsureLinkedAsync(Customer customer, long companyId, long? branchId, CancellationToken cancellationToken = default)
    {
        var binding = await asaasCustomerRepository.GetByCustomerIdAndCompanyIdAsync(customer.Id, companyId, cancellationToken);
        if (binding is not null)
            return Result.Success(binding.AsaasCustomerId);

        if (string.IsNullOrWhiteSpace(customer.Cpf) || string.IsNullOrWhiteSpace(customer.Email))
            return Result.Failure<string>(
                Error.Validation("Customer.IncompleteProfile", "CPF e e-mail são obrigatórios para pagamento online. Complete seu cadastro."));

        await asaasService.ConfigureForTenantAsync(companyId, branchId, cancellationToken);

        string asaasCustomerId;
        try
        {
            asaasCustomerId = await asaasService.CreateCustomerAsync(
                customer.Name, customer.Cpf!, customer.Email!, customer.Phone, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<string>(Error.Failure("AsaasApi.CreateCustomerFailed", $"Falha ao cadastrar cliente no Asaas: {ex.Message}"));
        }

        var newBindingResult = AsaasIntegrationCustomer.Create(customer.Id, companyId, asaasCustomerId);
        if (newBindingResult.IsFailure)
            return Result.Failure<string>(newBindingResult.Error);

        await asaasCustomerRepository.AddAsync(newBindingResult.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(asaasCustomerId);
    }
}
