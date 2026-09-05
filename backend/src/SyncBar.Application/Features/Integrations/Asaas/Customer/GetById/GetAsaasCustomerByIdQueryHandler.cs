using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Customer.GetById
{
    internal sealed class GetAsaasCustomerByIdQueryHandler
        : BaseQueryHandler<GetAsaasCustomerByIdQuery, AsaasIntegrationCustomerResponse>
    {
        private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository;

        public GetAsaasCustomerByIdQueryHandler(
            IAsaasIntegrationCustomerRepository asaasCustomerRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _asaasCustomerRepository = asaasCustomerRepository;
        }

        public override async Task<Result<AsaasIntegrationCustomerResponse>> Handle(
            GetAsaasCustomerByIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAsaasCustomerByIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var customer = await _asaasCustomerRepository.GetByIdAsync(
                        request.Id,
                        cancellationToken);

                    if (customer is null)
                    {
                        return Result.Failure<AsaasIntegrationCustomerResponse>(
                            Error.NotFound(
                                "AsaasCustomer.NotFound",
                                $"Registro do cliente com Id {request.Id} não foi encontrado."));
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
