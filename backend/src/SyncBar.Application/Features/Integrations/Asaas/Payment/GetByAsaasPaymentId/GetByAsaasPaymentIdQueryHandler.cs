using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId
{
    internal sealed class GetByAsaasPaymentIdQueryHandler
        : BaseQueryHandler<GetByAsaasPaymentIdQuery, AsaasIntegrationPaymentResponse>
    {
        private readonly IAsaasIntegrationPaymentRepository _paymentRepository;

        public GetByAsaasPaymentIdQueryHandler(
            IAsaasIntegrationPaymentRepository paymentRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _paymentRepository = paymentRepository;
        }

        public override async Task<Result<AsaasIntegrationPaymentResponse>> Handle(
            GetByAsaasPaymentIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetByAsaasPaymentIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var payment = await _paymentRepository.GetByAsaasPaymentIdAsync(
                        request.AsaasPaymentId,
                        cancellationToken);

                    if (payment is null)
                    {
                        return Result.Failure<AsaasIntegrationPaymentResponse>(
                            Error.NotFound(
                                "AsaasPayment.NotFound",
                                $"Cobrança com o AsaasPaymentId '{request.AsaasPaymentId}' não foi encontrada."));
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
