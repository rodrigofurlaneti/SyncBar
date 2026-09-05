using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Application.Abstractions.Integrations.Asaas;

namespace SyncBar.Application.Features.Integrations.Asaas.Payment.Delete
{
    internal sealed class DeleteAsaasPaymentCommandHandler : BaseCommandHandler<DeleteAsaasPaymentCommand>
    {
        private readonly IAsaasIntegrationPaymentRepository _paymentRepository;
        private readonly IAsaasService _asaasService;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteAsaasPaymentCommandHandler(
            IAsaasIntegrationPaymentRepository paymentRepository,
            IAsaasService asaasService,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _paymentRepository = paymentRepository;
            _asaasService = asaasService;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(DeleteAsaasPaymentCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(DeleteAsaasPaymentCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var payment = await _paymentRepository.GetByIdForUpdateAsync(request.PaymentId, cancellationToken);

                    if (payment is null)
                    {
                        return Result.Failure(
                            Error.NotFound(
                                "AsaasPayment.NotFound",
                                $"Cobrança com o Id {request.PaymentId} não foi encontrada."));
                    }

                    // Cobranças já liquidadas não podem ser canceladas/removidas
                    if (payment.Status is "RECEIVED" or "CONFIRMED")
                    {
                        return Result.Failure(
                            Error.Conflict(
                                "AsaasPayment.CannotDeletePaid",
                                "Cobranças já liquidadas ou confirmadas não podem ser excluídas. Utilize a rotina de estorno."));
                    }

                    // Cancela ou remove a cobrança no gateway Asaas (DELETE /v3/payments/{id})
                    try
                    {
                        await _asaasService.DeletePaymentAsync(payment.AsaasPaymentId, cancellationToken);
                    }
                    catch (HttpRequestException ex)
                    {
                        return Result.Failure(
                            Error.Failure("AsaasApi.DeleteFailed", $"Falha ao cancelar cobrança no Asaas: {ex.Message}"));
                    }

                    // Remoção no banco de dados local
                    _paymentRepository.Delete(payment);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
