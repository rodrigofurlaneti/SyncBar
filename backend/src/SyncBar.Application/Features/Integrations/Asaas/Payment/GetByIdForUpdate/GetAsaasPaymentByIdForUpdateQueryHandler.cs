using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetByIdForUpdate
{
    internal sealed class GetAsaasPaymentByIdForUpdateQueryHandler
        : BaseQueryHandler<GetAsaasPaymentByIdForUpdateQuery, AsaasIntegrationPaymentResponse>
    {
        private readonly IAsaasIntegrationPaymentRepository _paymentRepository;

        public GetAsaasPaymentByIdForUpdateQueryHandler(
            IAsaasIntegrationPaymentRepository paymentRepository,
            ILogTrackerRepository logRepository)
            : base(logRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public override async Task<Result<AsaasIntegrationPaymentResponse>> Handle(
            GetAsaasPaymentByIdForUpdateQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAsaasPaymentByIdForUpdateQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var payment = await _paymentRepository.GetByIdForUpdateAsync(
                        request.Id,
                        cancellationToken);

                    if (payment is null)
                    {
                        return Result.Failure<AsaasIntegrationPaymentResponse>(
                            Error.NotFound(
                                "AsaasPayment.NotFound",
                                $"Pagamento com Id {request.Id} não foi encontrado para atualização."));
                    }

                    var response = new AsaasIntegrationPaymentResponse(
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
                        payment.IsActive);

                    return Result.Success(response);
                });
        }
    }
}
