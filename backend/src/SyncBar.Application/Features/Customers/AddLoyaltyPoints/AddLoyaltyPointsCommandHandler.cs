using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Customers.AddLoyaltyPoints;

internal sealed class AddLoyaltyPointsCommandHandler(
    ICustomerRepository customerRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<AddLoyaltyPointsCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(AddLoyaltyPointsCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(AddLoyaltyPointsCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário que está executando a ação, preencha:
                // userIdBox.Value = request.UserId;

                var customer = await customerRepository.GetByIdForUpdateAsync(request.CustomerId, cancellationToken);
                if (customer is null || !customer.IsActive)
                    return Result.Failure(new Error("Customer.NotFound", "Customer not found."));

                var result = customer.AddLoyaltyPoints(request.Points);
                if (result.IsFailure)
                    return result;

                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}