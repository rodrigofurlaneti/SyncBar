using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentIdForUpdate
{
    internal sealed class GetByAsaasPaymentIdForUpdateQueryHandler
        : BaseQueryHandler<GetByAsaasPaymentIdForUpdateQuery, AsaasIntegrationPaymentResponse>
    {
        private readonly IAsaasIntegrationPaymentRepository _paymentRepository;

        public GetByAsaasPaymentIdForUpdateQueryHandler(
            IAsaasIntegrationPaymentRepository paymentRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _paymentRepository = paymentRepository;
        }

        public override async Task<Result<AsaasIntegrationPaymentResponse>> Handle(
            GetByAsaasPaymentIdForUpdateQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetByAsaasPaymentIdForUpdateQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    // Carrega a entidade COM rastreamento (tracking) no DbContext
                    var payment = await _paymentRepository.GetByAsaasPaymentIdForUpdateAsync(
                        request.AsaasPaymentId,
                        cancellationToken);

                    if (payment is null)
                    {
                        return Result.Failure<AsaasIntegrationPaymentResponse>(
                            Error.NotFound(
                                "AsaasPayment.NotFound",
                                $"Cobrança com o AsaasPaymentId '{request.AsaasPaymentId}' não foi encontrada para atualização."));
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
