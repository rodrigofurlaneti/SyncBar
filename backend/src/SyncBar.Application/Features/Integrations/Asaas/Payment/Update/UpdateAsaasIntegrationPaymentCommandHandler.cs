using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Payment.Update
{
    internal sealed class UpdateAsaasIntegrationPaymentCommandHandler
        : BaseCommandHandler<UpdateAsaasIntegrationPaymentCommand>
    {
        private readonly IAsaasIntegrationPaymentRepository _paymentRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateAsaasIntegrationPaymentCommandHandler(
            IAsaasIntegrationPaymentRepository paymentRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _paymentRepository = paymentRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            UpdateAsaasIntegrationPaymentCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(UpdateAsaasIntegrationPaymentCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var payment = await _paymentRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

                    if (payment is null)
                    {
                        return Result.Failure(
                            Error.NotFound(
                                "AsaasPayment.NotFound",
                                $"Cobrança com Id {request.Id} não foi encontrada."));
                    }

                    // Atualiza o status e, se fornecidos, dados de liquidação
                    payment.UpdateStatus(request.Status, request.NetValue, request.PaymentDate);

                    // Atualiza dados Pix se informados
                    if (!string.IsNullOrWhiteSpace(request.PixQrCodeBase64) || !string.IsNullOrWhiteSpace(request.PixPayload))
                    {
                        payment.SetPixDetails(request.PixQrCodeBase64, request.PixPayload);
                    }

                    // Atualiza URLs auxiliares se informadas
                    if (!string.IsNullOrWhiteSpace(request.InvoiceUrl) || !string.IsNullOrWhiteSpace(request.BankSlipUrl))
                    {
                        payment.SetUrls(request.InvoiceUrl, request.BankSlipUrl);
                    }

                    _paymentRepository.Update(payment);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
