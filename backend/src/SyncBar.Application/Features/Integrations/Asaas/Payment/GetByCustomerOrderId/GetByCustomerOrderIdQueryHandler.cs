using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetByCustomerOrderId
{
    internal sealed class GetByCustomerOrderIdQueryHandler
        : BaseQueryHandler<GetByCustomerOrderIdQuery, AsaasIntegrationPaymentResponse>
    {
        private readonly IAsaasIntegrationPaymentRepository _paymentRepository;

        public GetByCustomerOrderIdQueryHandler(
            IAsaasIntegrationPaymentRepository paymentRepository,
            ILogTrackerRepository logRepository)
            : base(logRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public override async Task<Result<AsaasIntegrationPaymentResponse>> Handle(
            GetByCustomerOrderIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetByCustomerOrderIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var payment = await _paymentRepository.GetByCustomerOrderIdAsync(
                        request.CustomerOrderId,
                        cancellationToken);

                    if (payment is null)
                    {
                        return Result.Failure<AsaasIntegrationPaymentResponse>(
                            Error.NotFound(
                                "AsaasPayment.NotFound",
                                $"Nenhum pagamento integrado foi encontrado para o pedido {request.CustomerOrderId}."));
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
