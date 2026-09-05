using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Asaas.Customer.Update
{
    internal sealed class UpdateAsaasIntegrationCustomerCommandHandler : BaseCommandHandler<UpdateAsaasIntegrationCustomerCommand>
    {
        private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateAsaasIntegrationCustomerCommandHandler(
            IAsaasIntegrationCustomerRepository asaasCustomerRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _asaasCustomerRepository = asaasCustomerRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(UpdateAsaasIntegrationCustomerCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(UpdateAsaasIntegrationCustomerCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var customer = await _asaasCustomerRepository.GetByIdForUpdateAsync(request.Id, cancellationToken)
                        ?? await _asaasCustomerRepository.GetByIdAsync(request.Id, cancellationToken);

                    if (customer is null)
                    {
                        return Result.Failure(
                            Error.NotFound(
                                "AsaasCustomer.NotFound",
                                $"Registro do cliente com Id {request.Id} não foi encontrado."));
                    }

                    customer.UpdateAsaasCustomerId(request.NewAsaasCustomerId);

                    _asaasCustomerRepository.Update(customer);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
