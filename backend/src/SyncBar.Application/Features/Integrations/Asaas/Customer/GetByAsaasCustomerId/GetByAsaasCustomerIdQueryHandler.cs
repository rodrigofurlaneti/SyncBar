using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Customer.GetByAsaasCustomerId
{
    internal sealed class GetByAsaasCustomerIdQueryHandler
        : BaseQueryHandler<GetByAsaasCustomerIdQuery, AsaasIntegrationCustomerResponse>
    {
        private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository;

        public GetByAsaasCustomerIdQueryHandler(
            IAsaasIntegrationCustomerRepository asaasCustomerRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _asaasCustomerRepository = asaasCustomerRepository;
        }

        public override async Task<Result<AsaasIntegrationCustomerResponse>> Handle(
            GetByAsaasCustomerIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetByAsaasCustomerIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var customer = await _asaasCustomerRepository.GetByAsaasCustomerIdAsync(
                        request.AsaasCustomerId,
                        cancellationToken);

                    if (customer is null)
                    {
                        return Result.Failure<AsaasIntegrationCustomerResponse>(
                            Error.NotFound(
                                "AsaasCustomer.NotFound",
                                $"Vínculo com o AsaasCustomerId '{request.AsaasCustomerId}' não foi encontrado."));
                    }

                    var response = new AsaasIntegrationCustomerResponse(
                        customer.Id,
                        customer.CustomerId,
                        customer.CompanyId,
                        customer.AsaasCustomerId,
                        customer.CreatedAt,
                        customer.IsActive);

                    return Result.Success(response);
                });
        }
    }
}
