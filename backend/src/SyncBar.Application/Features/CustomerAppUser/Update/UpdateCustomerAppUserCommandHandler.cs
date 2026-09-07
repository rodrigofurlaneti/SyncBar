using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.CustomerAppUser.Update;

internal sealed class UpdateCustomerAppUserCommandHandler : BaseCommandHandler<UpdateCustomerAppUserCommand>
{
    private readonly ICustomerAppUserRepository _customerAppUserRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerAppUserCommandHandler(
        ICustomerAppUserRepository customerAppUserRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _customerAppUserRepository = customerAppUserRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(UpdateCustomerAppUserCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(UpdateCustomerAppUserCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var entity = await _customerAppUserRepository.GetByIdAsync(request.Id, cancellationToken);
                if (entity is null || !entity.IsActive)
                    return Result.Failure(new Error("CustomerAppUser.NotFound", "Customer app user not found."));

                // Atualizando os dados básicos da entidade
                entity.UpdateDetails(
                    request.CompanyId,
                    request.BranchId,
                    request.CustomerId,
                    request.UserName,
                    request.Email
                );

                // Atualiza a senha apenas se informada
                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                    entity.ChangePasswordHash(passwordHash);
                }

                await _customerAppUserRepository.UpdateAsync(entity, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success();
            });
    }
}