using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId
{
    internal sealed class GetAllAsaasCustomersByCompanyIdQueryHandler
        : BaseQueryHandler<GetAllAsaasCustomersByCompanyIdQuery, IReadOnlyList<AsaasIntegrationCustomerResponse>>
    {
        private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository;

        public GetAllAsaasCustomersByCompanyIdQueryHandler(
            IAsaasIntegrationCustomerRepository asaasCustomerRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _asaasCustomerRepository = asaasCustomerRepository;
        }

        public override async Task<Result<IReadOnlyList<AsaasIntegrationCustomerResponse>>> Handle(
            GetAllAsaasCustomersByCompanyIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAllAsaasCustomersByCompanyIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var customers = await _asaasCustomerRepository.GetAllByCompanyIdAsync(
                        request.CompanyId,
                        cancellationToken);

                    var response = customers
                        .Select(c => new AsaasIntegrationCustomerResponse(
                            c.Id,
                            c.CustomerId,
                            c.CompanyId,
                            c.AsaasCustomerId,
                            c.CreatedAt,
                            c.IsActive))
                        .ToList();

                    return Result.Success<IReadOnlyList<AsaasIntegrationCustomerResponse>>(response);
                });
        }
    }
}
