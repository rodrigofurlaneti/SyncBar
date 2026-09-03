using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Shift.CloseShift;

// Fechamento/consolidacao do turno comercial: busca todas as CashSession da
// filial desde a abertura do turno ate agora, valida que nenhuma esta aberta
// e totaliza fundo de troco, esperado, realizado e diferenca geral. Cada
// CashSession consolidada gera uma linha de auditoria em ShiftClosingSession.
internal sealed class CloseShiftClosingCommandHandler : BaseCommandHandler<CloseShiftClosingCommand, ShiftClosingResponse>
{
    private readonly IShiftClosingRepository _shiftClosingRepository;
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly IShiftClosingSessionRepository _shiftClosingSessionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CloseShiftClosingCommandHandler(
        IShiftClosingRepository shiftClosingRepository,
        ICashSessionRepository cashSessionRepository,
        IShiftClosingSessionRepository shiftClosingSessionRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _shiftClosingRepository = shiftClosingRepository;
        _cashSessionRepository = cashSessionRepository;
        _shiftClosingSessionRepository = shiftClosingSessionRepository;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result<ShiftClosingResponse>> Handle(CloseShiftClosingCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(CloseShiftClosingCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se o IP estiver disponível no Command
            async (userIdBox) =>
            {
                // Registra o ID do funcionário no log para sabermos quem fechou o turno.
                userIdBox.Value = request.ClosedByEmployeeId;

                var shift = await _shiftClosingRepository.GetByIdForUpdateAsync(request.ShiftClosingId, cancellationToken);
                if (shift is null || !shift.IsActive)
                    return Result.Failure<ShiftClosingResponse>(new Error("ShiftClosing.NotFound", "Shift closing not found."));

                var periodEnd = DateTime.Now;

                // Consolidacao automatica: todas as CashSession da filial abertas dentro do
                // periodo do turno (do momento em que o turno foi aberto ate agora).
                var cashSessions = await _cashSessionRepository.GetByBranchAndPeriodAsync(
                    shift.BranchId, shift.PeriodStart, periodEnd, cancellationToken);

                // O proprio Close() valida (e bloqueia) caixas ainda abertos pendentes no periodo.
                var result = shift.Close(request.ClosedByEmployeeId, periodEnd, cashSessions, request.Notes);
                if (result.IsFailure)
                    return Result.Failure<ShiftClosingResponse>(result.Error);

                var links = new List<ShiftClosingSession>();
                foreach (var session in cashSessions.Where(s => s.IsActive))
                {
                    var link = ShiftClosingSession.Create(shift.Id, session.Id);
                    if (link.IsSuccess)
                        links.Add(link.Value);
                }

                if (links.Count > 0)
                    await _shiftClosingSessionRepository.AddRangeAsync(links, cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(new ShiftClosingResponse(
                    shift.Id,
                    shift.BranchId,
                    shift.ShiftClosingStatusId,
                    shift.OpenedByEmployeeId,
                    shift.ClosedByEmployeeId,
                    shift.PeriodStart,
                    shift.PeriodEnd,
                    shift.CashSessionsCount,
                    shift.TotalOpeningAmount,
                    shift.TotalExpectedAmount,
                    shift.TotalRealizedAmount,
                    shift.TotalDifferenceAmount,
                    shift.Notes));
            });
}
