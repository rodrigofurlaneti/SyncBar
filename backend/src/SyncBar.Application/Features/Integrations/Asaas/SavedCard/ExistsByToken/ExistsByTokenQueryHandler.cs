using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.ExistsByToken
{
    internal sealed class ExistsByTokenQueryHandler : BaseQueryHandler<ExistsByTokenQuery, bool>
    {
        private readonly IAsaasIntegrationSavedCardRepository _savedCardRepository;

        public ExistsByTokenQueryHandler(
            IAsaasIntegrationSavedCardRepository savedCardRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _savedCardRepository = savedCardRepository;
        }

        public override async Task<Result<bool>> Handle(
            ExistsByTokenQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(ExistsByTokenQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var exists = await _savedCardRepository.ExistsByTokenAsync(
                        request.CreditCardToken,
                        cancellationToken);

                    return Result.Success(exists);
                });
        }
    }
}
