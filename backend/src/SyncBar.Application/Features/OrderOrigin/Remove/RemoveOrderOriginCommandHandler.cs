using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.OrderOrigin.Remove
{
    internal sealed class RemoveOrderOriginCommandHandler : BaseCommandHandler<RemoveOrderOriginCommand>
    {
        private readonly IOrderOriginRepository _orderOriginRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveOrderOriginCommandHandler(
            IOrderOriginRepository orderOriginRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderOriginRepository = orderOriginRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(RemoveOrderOriginCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(RemoveOrderOriginCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    // Busca a entidade rastreada pelo EF Core para remoção
                    var orderOrigin = await _orderOriginRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

                    if (orderOrigin is null)
                    {
                        return Result.Failure(new Error(
                            "OrderOrigin.NotFound",
                            "Origem de pedido não encontrada."));
                    }

                    // Executa a remoção (nota: caso a origem esteja vinculada a pedidos, 
                    // a restrição de chave estrangeira do banco impedirá a exclusão garantindo a integridade)
                    _orderOriginRepository.Remove(orderOrigin);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
