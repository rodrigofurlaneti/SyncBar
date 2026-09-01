namespace SyncBar.Application.Features.Orders
{
    public sealed record TableReadingValidationSettingResponse(
        bool IsCameraInputEnabled,
        bool IsBarcodeEnabled,
        bool IsQrCodeEnabled);
}
