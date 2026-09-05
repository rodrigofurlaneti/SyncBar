using SyncBar.Application.Abstractions.Integrations.Asaas;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Checkout.Shared
{
    public interface ICheckoutOrderPreparer
    {
        Task<Result<CheckoutOrderPreparation>> PrepareAsync(long customerOrderId, CancellationToken cancellationToken = default);
    }

    public sealed record CheckoutOrderPreparation(CustomerOrder Order, Customer Customer, Branch Branch, string AsaasCustomerId);

    internal sealed class CheckoutOrderPreparer(
        ICustomerOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IBranchRepository branchRepository,
        IAsaasCustomerProvisioningService customerProvisioner,
        IAsaasService asaasService,
        TimeProvider timeProviderCustom,
        IUnitOfWork unitOfWork) : ICheckoutOrderPreparer
    {
        public async Task<Result<CheckoutOrderPreparation>> PrepareAsync(long customerOrderId, CancellationToken cancellationToken = default)
        {
            var order = await orderRepository.GetByIdForUpdateAsync(customerOrderId, cancellationToken);
            if (order is null || !order.IsActive)
                return Result.Failure<CheckoutOrderPreparation>(Error.NotFound("CustomerOrder.NotFound", "Pedido não encontrado."));

            if (order.OrderStatusId is OrderStatusIds.Aberto or OrderStatusIds.EmAndamento)
            {
                var closeResult = order.Close(0m, timeProviderCustom.GetLocalNow().DateTime);
                if (closeResult.IsFailure)
                    return Result.Failure<CheckoutOrderPreparation>(closeResult.Error);

                await unitOfWork.CommitAsync(cancellationToken);
            }
            else if (order.OrderStatusId != OrderStatusIds.AguardandoPagamento)
            {
                return Result.Failure<CheckoutOrderPreparation>(
                    Error.Validation("CustomerOrder.NotPayable", "Pedido não pode ser pago neste estado."));
            }

            if (!order.CustomerId.HasValue)
                return Result.Failure<CheckoutOrderPreparation>(
                    Error.Validation("CustomerOrder.CustomerRequired", "Pedido sem cliente vinculado; não é possível cobrar online."));

            var customer = await customerRepository.GetByIdAsync(order.CustomerId.Value, cancellationToken);
            if (customer is null)
                return Result.Failure<CheckoutOrderPreparation>(Error.NotFound("Customer.NotFound", "Cliente não encontrado."));

            var branch = await branchRepository.GetByIdAsync(order.BranchId, cancellationToken);
            if (branch is null)
                return Result.Failure<CheckoutOrderPreparation>(
                    Error.NotFound("Branch.NotFound", $"Filial com ID {order.BranchId} não foi encontrada."));

            // Aplica a credencial Asaas da filial/empresa antes de qualquer chamada ao gateway
            // feita pelo restante do fluxo de checkout (tokenização de cartão, busca de linha
            // digitável do boleto, etc. — todas usam a mesma instância de IAsaasService deste escopo).
            await asaasService.ConfigureForTenantAsync(branch.CompanyId, order.BranchId, cancellationToken);

            var provisionResult = await customerProvisioner.EnsureLinkedAsync(customer, branch.CompanyId, order.BranchId, cancellationToken);
            if (provisionResult.IsFailure)
                return Result.Failure<CheckoutOrderPreparation>(provisionResult.Error);

            return Result.Success(new CheckoutOrderPreparation(order, customer, branch, provisionResult.Value));
        }
    }
}
