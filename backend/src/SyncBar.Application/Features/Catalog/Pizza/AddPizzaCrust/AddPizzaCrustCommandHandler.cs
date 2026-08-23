using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Pizza.AddPizzaCrust;

internal sealed class AddPizzaCrustCommandHandler(
    IPizzaConfigurationRepository pizzaConfigurationRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<AddPizzaCrustCommand, long>(logRepository, unitOfWork)
{
    public override Task<Result<long>> Handle(AddPizzaCrustCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(AddPizzaCrustCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                var configuration = await pizzaConfigurationRepository.GetByIdForUpdateAsync(request.PizzaConfigurationId, cancellationToken);
                if (configuration is null || !configuration.IsActive)
                    return Result.Failure<long>(new Error("PizzaConfiguration.NotFound", "Pizza configuration not found."));

                var crust = configuration.AddCrust(request.Name, request.ExtraPrice, request.DisplayOrder);
                if (crust.IsFailure)
                    return Result.Failure<long>(crust.Error);

                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Success(crust.Value.Id);
            });
}
