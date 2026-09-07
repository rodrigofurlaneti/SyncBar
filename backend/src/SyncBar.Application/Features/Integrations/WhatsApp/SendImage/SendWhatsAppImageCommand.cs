using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.WhatsApp.SendImage
{
    public sealed record SendWhatsAppImageCommand(string PhoneNumber, string Message, string FileUrl) : ICommand;
}
