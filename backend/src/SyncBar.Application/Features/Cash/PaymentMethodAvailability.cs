using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Cash;

public interface IPaymentMethodAvailability
{
    Task<Result> ValidateAsync(long branchId, IReadOnlyCollection<long> methods, CancellationToken ct);
    Task<Result> ValidateOrderAsync(long orderId, IReadOnlyCollection<long> methods, CancellationToken ct);
}

internal sealed class PaymentMethodAvailability(IBranchRepository branches, IBranchPaymentMethodSettingRepository settings, ICustomerOrderRepository orders) : IPaymentMethodAvailability
{
    public async Task<Result> ValidateOrderAsync(long orderId, IReadOnlyCollection<long> methods, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(orderId, ct);
        return order is null || !order.IsActive ? Result.Failure(new Error("CustomerOrder.NotFound", "Pedido não encontrado."))
            : await ValidateAsync(order.BranchId, methods, ct);
    }
    public async Task<Result> ValidateAsync(long branchId, IReadOnlyCollection<long> methods, CancellationToken ct)
    {
        var branch = await branches.GetByIdAsync(branchId, ct);
        if (branch is null || !branch.IsActive) return Result.Failure(new Error("Branch.NotFound", "Filial não encontrada."));
        var flags = await settings.GetByBranchOrCompanyFallbackAsync(branch.CompanyId, branchId, ct);
        if (flags is null) return Result.Success();
        var disabled = methods.Any(id => id switch
        {
            2 => !flags.EnableCreditCard,
            3 => !flags.EnableDebitCard,
            4 => !flags.EnablePix,
            8 => !flags.EnableBoleto,
            9 => !flags.EnableCashMachine,
            _ => false
        });
        return disabled ? Result.Failure(new Error("PaymentMethod.Disabled", "Esta forma de pagamento está desativada na filial.")) : Result.Success();
    }
}
