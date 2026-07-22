using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Customers.AddLoyaltyPoints;

internal sealed class AddLoyaltyPointsCommandHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<AddLoyaltyPointsCommand>
{
    public async Task<Result> Handle(AddLoyaltyPointsCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdForUpdateAsync(request.CustomerId, cancellationToken);
        if (customer is null || !customer.IsActive)
            return Result.Failure(new Error("Customer.NotFound", "Customer not found."));

        var result = customer.AddLoyaltyPoints(request.Points);
        if (result.IsFailure)
            return result;

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
