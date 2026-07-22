using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Customers.Create;

internal sealed class CreateCustomerCommandHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateCustomerCommand, long>
{
    public async Task<Result<long>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = Customer.Create(request.CompanyId, request.Name, request.Phone, request.Cpf, request.Email);
        if (customer.IsFailure)
            return Result.Failure<long>(customer.Error);

        await customerRepository.AddAsync(customer.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(customer.Value.Id);
    }
}
