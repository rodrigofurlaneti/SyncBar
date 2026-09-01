using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.SetTableReadingValidation;

/// <summary>
/// Liga/desliga, para todas as mesas de uma filial, os três cenários de validação
/// obrigatória na leitura da comanda/mesa: captura por câmera, leitura de código de
/// barras e leitura de QR Code. Espelha o padrão de <c>SetQrViewEnabledCommand</c>.
/// </summary>
public sealed record SetTableReadingValidationCommand(
    long BranchId,
    bool IsCameraInputEnabled,
    bool IsBarcodeEnabled,
    bool IsQrCodeEnabled) : ICommand;
