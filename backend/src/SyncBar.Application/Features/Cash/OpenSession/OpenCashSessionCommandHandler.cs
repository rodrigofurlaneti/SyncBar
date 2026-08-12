using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Cash.OpenSession;

internal sealed class OpenCashSessionCommandHandler : BaseCommandHandler<OpenCashSessionCommand, long>
{
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OpenCashSessionCommandHandler(
        ICashSessionRepository cashSessionRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _cashSessionRepository = cashSessionRepository;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result<long>> Handle(OpenCashSessionCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(OpenCashSessionCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se o IP estiver disponível no Command
            async (userIdBox) =>
            {
                // Registra o ID do funcionário no log para rastrearmos quem abriu o caixa
                userIdBox.Value = request.OpenedByEmployeeId;

                // Uma unica sessao aberta por caixa.
                var open = await _cashSessionRepository.GetOpenByCashRegisterAsync(request.CashRegisterId, cancellationToken);
                if (open is not null)
                    return Result.Failure<long>(new Error("CashSession.AlreadyOpen", "This cash register already has an open session."));

                var session = CashSession.Open(request.CashRegisterId, request.OpenedByEmployeeId, request.OpeningAmount);
                if (session.IsFailure)
                    return Result.Failure<long>(session.Error);

                await _cashSessionRepository.AddAsync(session.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(session.Value.Id);
            });
}