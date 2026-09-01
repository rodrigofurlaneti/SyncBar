using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Storage;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.PublicOrdering.ValidateComandaReading;

internal sealed class ValidateComandaReadingCommandHandler : BaseCommandHandler<ValidateComandaReadingCommand>
{
    private readonly IDiningTableRepository _diningTableRepository;
    private readonly IComandaRepository _comandaRepository;
    private readonly IImageStorage _imageStorage;
    private readonly ILogTrackerRepository _logRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ValidateComandaReadingCommandHandler(
        IDiningTableRepository diningTableRepository,
        IComandaRepository comandaRepository,
        IImageStorage imageStorage,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _diningTableRepository = diningTableRepository;
        _comandaRepository = comandaRepository;
        _imageStorage = imageStorage;
        _logRepository = logRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(ValidateComandaReadingCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ValidateComandaReadingCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                var table = await _diningTableRepository.GetByQrTokenAsync(request.TableToken, cancellationToken);
                if (table is null)
                    return Result.Failure(new Error("DiningTable.InvalidToken", "Invalid or expired QR code."));

                var comanda = await _comandaRepository.GetByCodeAsync(table.BranchId, request.ComandaCode, cancellationToken);
                if (comanda is null || !comanda.IsActive)
                    return Result.Failure(new Error("Comanda.NotFound", "Comanda não encontrada."));

                var method = request.Method.ToLowerInvariant();
                string? proofDescription;

                if (method == "camera")
                {
                    var bytes = DecodeBase64Photo(request.PhotoBase64!);
                    if (bytes is null)
                        return Result.Failure(new Error("ComandaReadingValidation.InvalidPhoto", "Foto inválida."));

                    var photoUrl = await _imageStorage.SaveComandaValidationPhotoAsync(
                        table.Id, comanda.Code, ".jpg", bytes, cancellationToken);
                    proofDescription = $"foto salva em {photoUrl}";
                }
                else if (method is "barcode" or "qrcode")
                {
                    proofDescription = $"código lido: {request.ScannedValue}";
                }
                else
                {
                    return Result.Failure(new Error("ComandaReadingValidation.InvalidMethod", "Método de validação inválido."));
                }

                // Registro de auditoria/rastreabilidade — reaproveita o LogTracker (mecanismo de
                // trilha já existente no app) em vez de criar uma tabela dedicada, já que hoje só
                // precisamos de "quando/onde/como" e não de consultas estruturadas por método.
                var auditLog = new LogTracker(0)
                {
                    AppUserId = userIdBox.Value,
                    DirectoryName = "PublicOrdering/ValidateComandaReading",
                    ClassName = nameof(ValidateComandaReadingCommandHandler),
                    MethodName = nameof(Handle),
                    IsSuccess = true,
                    Message = $"Validação de leitura de comanda ({method}) — Mesa {table.Number}, Comanda {comanda.Code}, {proofDescription}",
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                };
                await _logRepository.AddAsync(auditLog, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success();
            });
    }

    private static byte[]? DecodeBase64Photo(string photoBase64)
    {
        var commaIndex = photoBase64.IndexOf(',');
        var payload = photoBase64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0
            ? photoBase64[(commaIndex + 1)..]
            : photoBase64;

        try
        {
            return Convert.FromBase64String(payload);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
