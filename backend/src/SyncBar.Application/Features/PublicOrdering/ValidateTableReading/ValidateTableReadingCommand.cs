using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.PublicOrdering.ValidateTableReading;

/// <summary>
/// Registra a comprovação de leitura da MESA feita pelo cliente (câmera, código de
/// barras ou QR Code), exigida antes de liberar QUALQUER pedido direto na mesa quando a
/// "Visualização do Cliente (QR Code)" está desligada (<c>DiningTable.IsQrViewEnabled
/// = false</c> — nesse caso não existe fluxo de comanda para o cliente) e algum dos
/// flags <c>IsCameraInputEnabled</c>/<c>IsBarcodeEnabled</c>/<c>IsQrCodeEnabled</c> está
/// ligado. Irmã de <see cref="ValidateComandaReading.ValidateComandaReadingCommand"/>,
/// mas sem código de comanda — a mesa já é identificada pelo próprio token do QR Code.
/// Sem autenticação — o "segredo" é o token do QR Code da mesa, igual às demais
/// features de <c>PublicOrdering</c>.
/// </summary>
/// <param name="Method">Um de: "camera", "barcode", "qrcode".</param>
/// <param name="ScannedValue">Obrigatório para "barcode"/"qrcode" — o valor lido pela câmera do celular.</param>
/// <param name="PhotoBase64">Obrigatório para "camera" — a foto tirada, como data URL ou base64 puro.</param>
public sealed record ValidateTableReadingCommand(
    Guid TableToken,
    string Method,
    string? ScannedValue,
    string? PhotoBase64) : ICommand;
