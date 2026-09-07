using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.CustomerAppUser.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.CustomerAppUser.Create;

internal sealed class CreateCustomerAppUserCommandHandler : BaseCommandHandler<CreateCustomerAppUserCommand, long>
{
    private readonly ICustomerAppUserRepository _customerAppUserRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerAppUserCommandHandler(
        ICustomerAppUserRepository customerAppUserRepository,
        ICustomerRepository customerRepository,
        ILogTrackerRepository logRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _customerAppUserRepository = customerAppUserRepository;
        _customerRepository = customerRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

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
                        request.Cpf,
                        request.Email
                    );
                    if (customerResult.IsFailure)
                        return Result.Failure<long>(customerResult.Error);

                    var customer = customerResult.Value;
                    await _customerRepository.AddAsync(customer, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);
                    customerId = customer.Id;
                }

                string passwordHash = _passwordHasher.Hash(request.Password);
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
                await _customerAppUserRepository.AddAsync(entity, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success(customerId ?? 0);
            });
    }
}