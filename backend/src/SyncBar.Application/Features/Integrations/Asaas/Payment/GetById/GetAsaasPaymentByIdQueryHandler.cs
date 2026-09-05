using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetById
{
    internal sealed class GetAsaasPaymentByIdQueryHandler
        : BaseQueryHandler<GetAsaasPaymentByIdQuery, AsaasIntegrationPaymentResponse>
    {
        private readonly IAsaasIntegrationPaymentRepository _paymentRepository;

        public GetAsaasPaymentByIdQueryHandler(
            IAsaasIntegrationPaymentRepository paymentRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _paymentRepository = paymentRepository;
        }

        public override async Task<Result<AsaasIntegrationPaymentResponse>> Handle(
            GetAsaasPaymentByIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAsaasPaymentByIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var payment = await _paymentRepository.GetByIdAsync(
                        request.Id,
                        cancellationToken);

                    if (payment is null)
                    {
                        return Result.Failure<AsaasIntegrationPaymentResponse>(
                            Error.NotFound(
                                "AsaasPayment.NotFound",
                                $"Pagamento com Id {request.Id} não foi encontrado."));
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
