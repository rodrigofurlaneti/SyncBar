using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetPendingByBranchId
{
    internal sealed class GetPendingAsaasPaymentsByBranchIdQueryHandler
        : BaseQueryHandler<GetPendingAsaasPaymentsByBranchIdQuery, IReadOnlyList<AsaasIntegrationPaymentResponse>>
    {
        private readonly IAsaasIntegrationPaymentRepository _paymentRepository;

        public GetPendingAsaasPaymentsByBranchIdQueryHandler(
            IAsaasIntegrationPaymentRepository paymentRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _paymentRepository = paymentRepository;
        }

        public override async Task<Result<IReadOnlyList<AsaasIntegrationPaymentResponse>>> Handle(
            GetPendingAsaasPaymentsByBranchIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetPendingAsaasPaymentsByBranchIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var payments = await _paymentRepository.GetPendingByBranchIdAsync(
                        request.BranchId,
                        cancellationToken);

                    var response = payments
                        .Select(payment => new AsaasIntegrationPaymentResponse(
                            payment.Id,
                            payment.BranchId,
                            payment.CustomerOrderId,
                            payment.CustomerId,
                            payment.AsaasPaymentId,
                            payment.BillingType,
                            payment.Status,
                            payment.Value,
                            payment.NetValue,
                            payment.DueDate,
                            payment.PaymentDate,
                            payment.PixQrCodeBase64,
                            payment.PixPayload,
                            payment.InvoiceUrl,
                            payment.BankSlipUrl,
                            payment.InstallmentCount,
                            payment.CreditCardToken,
                            payment.CreatedAt,
                            payment.IsActive))
                        .ToList();

                    return Result.Success<IReadOnlyList<AsaasIntegrationPaymentResponse>>(response);
                });
        }
    }
}
