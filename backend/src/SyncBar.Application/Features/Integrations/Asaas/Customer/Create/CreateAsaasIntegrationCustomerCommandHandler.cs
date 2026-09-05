using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Customer.Create
{
    internal sealed class CreateAsaasIntegrationCustomerCommandHandler : BaseCommandHandler<CreateAsaasIntegrationCustomerCommand, long>
    {
        private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateAsaasIntegrationCustomerCommandHandler(
            IAsaasIntegrationCustomerRepository asaasCustomerRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _asaasCustomerRepository = asaasCustomerRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result<long>> Handle(CreateAsaasIntegrationCustomerCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(CreateAsaasIntegrationCustomerCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    // Verifica se o vínculo entre o Customer local e a Company já existe
                    var alreadyExists = await _asaasCustomerRepository.ExistsAsync(
                        request.CustomerId,
                        request.CompanyId,
                        cancellationToken);

                    if (alreadyExists)
                    {
                        return Result.Failure<long>(
                            Error.Conflict(
                                "AsaasCustomer.AlreadyExists",
                                "O cliente já possui um vínculo com o Asaas cadastrado para esta empresa."));
                    }

                    // Cria a entidade de domínio através da fábrica estática
                    var customerResult = AsaasIntegrationCustomer.Create(
                        request.CustomerId,
                        request.CompanyId,
                        request.AsaasCustomerId);

                    if (customerResult.IsFailure)
                        return Result.Failure<long>(customerResult.Error);

                    await _asaasCustomerRepository.AddAsync(customerResult.Value, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success(customerResult.Value.Id);
                });
        }
    }
}
