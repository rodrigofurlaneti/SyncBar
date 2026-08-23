using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Pizza.AddPizzaEdge;

internal sealed class AddPizzaEdgeCommandHandler(
    IPizzaConfigurationRepository pizzaConfigurationRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<AddPizzaEdgeCommand, long>(logRepository, unitOfWork)
{
    public override Task<Result<long>> Handle(AddPizzaEdgeCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(AddPizzaEdgeCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                var configuration = await pizzaConfigurationRepository.GetByIdForUpdateAsync(request.PizzaConfigurationId, cancellationToken);
                if (configuration is null || !configuration.IsActive)
                    return Result.Failure<long>(new Error("PizzaConfiguration.NotFound", "Pizza configuration not found."));

                var edge = configuration.AddEdge(request.Name, request.ExtraPrice, request.DisplayOrder);
                if (edge.IsFailure)
                    return Result.Failure<long>(edge.Error);

                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Success(edge.Value.Id);
            });
}
