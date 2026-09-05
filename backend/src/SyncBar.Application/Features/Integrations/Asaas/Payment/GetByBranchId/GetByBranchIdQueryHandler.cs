using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Payment;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetByBranchId
{
    internal sealed class GetByBranchIdQueryHandler
        : BaseQueryHandler<GetByBranchIdQuery, IReadOnlyList<AsaasIntegrationPaymentResponse>>
    {
        private readonly IAsaasIntegrationPaymentRepository _paymentRepository;

        public GetByBranchIdQueryHandler(
            IAsaasIntegrationPaymentRepository paymentRepository,
            ILogTrackerRepository logRepository)
            : base(logRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public override async Task<Result<IReadOnlyList<AsaasIntegrationPaymentResponse>>> Handle(
            GetByBranchIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetByBranchIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var payments = await _paymentRepository.GetByBranchIdAsync(
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
