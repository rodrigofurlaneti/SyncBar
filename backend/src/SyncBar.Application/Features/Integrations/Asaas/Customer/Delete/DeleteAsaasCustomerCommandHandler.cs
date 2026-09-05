using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Integrations.Asaas;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Asaas.Customer.Delete
{
    internal sealed class DeleteAsaasCustomerCommandHandler : BaseCommandHandler<DeleteAsaasCustomerCommand>
    {
        private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository;
        private readonly IAsaasService _asaasService;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteAsaasCustomerCommandHandler(
            IAsaasIntegrationCustomerRepository asaasCustomerRepository,
            IAsaasService asaasService,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _asaasCustomerRepository = asaasCustomerRepository;
            _asaasService = asaasService;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(DeleteAsaasCustomerCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(DeleteAsaasCustomerCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    // Busca o cliente com tracking para remoção
                    var asaasCustomer = await _asaasCustomerRepository.GetByCustomerIdAndCompanyIdForUpdateAsync(
                        request.CustomerId,
                        request.CompanyId,
                        cancellationToken);

                    if (asaasCustomer is null)
                    {
                        return Result.Failure(
                            Error.NotFound(
                                "AsaasCustomer.NotFound",
                                $"Vínculo do cliente {request.CustomerId} para a empresa {request.CompanyId} não foi encontrado."));
                    }

                    // Remove o cadastro no gateway Asaas (DELETE /v3/customers/{id})
                    try
                    {
                        await _asaasService.DeleteCustomerAsync(asaasCustomer.AsaasCustomerId, cancellationToken);
                    }
                    catch (HttpRequestException ex)
                    {
                        return Result.Failure(
                            Error.Failure("AsaasApi.DeleteCustomerFailed", $"Falha ao remover cliente no Asaas: {ex.Message}"));
                    }

                    // Remoção no banco de dados local
                    _asaasCustomerRepository.Delete(asaasCustomer);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
