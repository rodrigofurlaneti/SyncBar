using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.CustomerAppUser.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.CustomerAppUser.Create;

internal sealed class CreateCustomerAppUserCommandHandler(
    ICustomerAppUserRepository customerAppUserRepository,
    ICustomerRepository customerRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<CreateCustomerAppUserCommand, long>(logRepository, unitOfWork)
{
    public override async Task<Result<long>> Handle(CreateCustomerAppUserCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CreateCustomerAppUserCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                long? customerId = request.CustomerId;
                if (!customerId.HasValue && !string.IsNullOrWhiteSpace(request.UserName))
                {
                    var customerResult = Customer.Create(
                        request.CompanyId,
                        request.UserName,
                        request.Phone,
                        null,
                        request.Email
                    );
                    if (customerResult.IsFailure)
                        return Result.Failure<long>(customerResult.Error);

                    var customer = customerResult.Value;
                    await customerRepository.AddAsync(customer, cancellationToken);
                    await unitOfWork.CommitAsync(cancellationToken);
                    customerId = customer.Id;
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                var customerAppUserResult = SyncBar.Domain.Entities.CustomerAppUser.Create(
                    request.CompanyId,
                    request.BranchId,
                    customerId,
                    request.UserName,
                    request.Email,
                    passwordHash
                );

                if (customerAppUserResult.IsFailure)
                    return Result.Failure<long>(customerAppUserResult.Error);

                var entity = customerAppUserResult.Value;
                await customerAppUserRepository.AddAsync(entity, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Success(customerId ?? 0);
            });
    }
}