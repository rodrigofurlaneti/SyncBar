using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Payment.ExistsByAsaasPaymentId
{
    internal sealed class ExistsByAsaasPaymentIdQueryHandler : BaseQueryHandler<ExistsByAsaasPaymentIdQuery, bool>
    {
        private readonly IAsaasIntegrationPaymentRepository _paymentRepository;

        public ExistsByAsaasPaymentIdQueryHandler(
            IAsaasIntegrationPaymentRepository paymentRepository,
            ILogTrackerRepository logRepository)
            : base(logRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public override async Task<Result<bool>> Handle(
            ExistsByAsaasPaymentIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(ExistsByAsaasPaymentIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var exists = await _paymentRepository.ExistsByAsaasPaymentIdAsync(
                        request.AsaasPaymentId,
                        cancellationToken);

                    return Result.Success(exists);
                });
        }
    }
}
