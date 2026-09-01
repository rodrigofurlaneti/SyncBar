using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Storage;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.PublicOrdering.ValidateTableReading;

internal sealed class ValidateTableReadingCommandHandler : BaseCommandHandler<ValidateTableReadingCommand>
{
    private readonly IDiningTableRepository _diningTableRepository;
    private readonly IImageStorage _imageStorage;
    private readonly ILogTrackerRepository _logRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ValidateTableReadingCommandHandler(
        IDiningTableRepository diningTableRepository,
        IImageStorage imageStorage,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _diningTableRepository = diningTableRepository;
        _imageStorage = imageStorage;
        _logRepository = logRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(ValidateTableReadingCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ValidateTableReadingCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                var table = await _diningTableRepository.GetByQrTokenAsync(request.TableToken, cancellationToken);
                if (table is null)
                    return Result.Failure(new Error("DiningTable.InvalidToken", "Invalid or expired QR code."));

                var method = request.Method.ToLowerInvariant();
                string? proofDescription;

                if (method == "camera")
                {
                    var bytes = DecodeBase64Photo(request.PhotoBase64!);
                    if (bytes is null)
                        return Result.Failure(new Error("TableReadingValidation.InvalidPhoto", "Foto inválida."));

                    var photoUrl = await _imageStorage.SaveTableValidationPhotoAsync(
                        table.Id, ".jpg", bytes, cancellationToken);
                    proofDescription = $"foto salva em {photoUrl}";
                }
                else if (method is "barcode" or "qrcode")
                {
                    proofDescription = $"código lido: {request.ScannedValue}";
                }
                else
                {
                    return Result.Failure(new Error("TableReadingValidation.InvalidMethod", "Método de validação inválido."));
                }

                // Registro de auditoria/rastreabilidade — reaproveita o LogTracker, igual à
                // validação de comanda. O número da mesa vai explícito na mensagem, já que
                // aqui (sem comanda) é a única forma de identificar pra onde o pedido vai.
                var auditLog = new LogTracker(0)
                {
                    AppUserId = userIdBox.Value,
                    DirectoryName = "PublicOrdering/ValidateTableReading",
                    ClassName = nameof(ValidateTableReadingCommandHandler),
                    MethodName = nameof(Handle),
                    IsSuccess = true,
                    Message = $"Validação de leitura de mesa ({method}) — Mesa {table.Number}, {proofDescription}",
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
