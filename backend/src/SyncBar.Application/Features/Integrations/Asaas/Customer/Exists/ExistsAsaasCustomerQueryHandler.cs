using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Customer.Exists
{
    internal sealed class ExistsAsaasCustomerQueryHandler : BaseQueryHandler<ExistsAsaasCustomerQuery, bool>
    {
        private readonly IAsaasIntegrationCustomerRepository _asaasCustomerRepository;

        public ExistsAsaasCustomerQueryHandler(
            IAsaasIntegrationCustomerRepository asaasCustomerRepository,
            ILogTrackerRepository logRepository)
            : base(logRepository)
        {
            _asaasCustomerRepository = asaasCustomerRepository;
        }

        public override async Task<Result<bool>> Handle(ExistsAsaasCustomerQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(ExistsAsaasCustomerQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var exists = await _asaasCustomerRepository.ExistsAsync(
                        request.CustomerId,
                        request.CompanyId,
                        cancellationToken);

                    return Result.Success(exists);
                });
        }
    }
}
