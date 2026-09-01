using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.PublicOrdering.ValidateComandaReading;

/// <summary>
/// Registra a comprovação de leitura da comanda feita pelo cliente (câmera, código de
/// barras ou QR Code), exigida conforme os flags de <c>DiningTable</c>
/// (<c>IsCameraInputEnabled</c>/<c>IsBarcodeEnabled</c>/<c>IsQrCodeEnabled</c>) antes de
/// liberar a consulta/lançamento na comanda. Sem autenticação — o "segredo" é o token do
/// QR Code da mesa, igual às demais features de <c>PublicOrdering</c>.
/// </summary>
/// <param name="Method">Um de: "camera", "barcode", "qrcode".</param>
/// <param name="ScannedValue">Obrigatório para "barcode"/"qrcode" — o valor lido pela câmera do celular.</param>
/// <param name="PhotoBase64">Obrigatório para "camera" — a foto tirada, como data URL ou base64 puro.</param>
public sealed record ValidateComandaReadingCommand(
    Guid TableToken,
    string ComandaCode,
    string Method,
    string? ScannedValue,
    string? PhotoBase64) : ICommand;
