using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SyncBar.Application.Abstractions.Notifications;
using SyncBar.Domain.Primitives;

namespace SyncBar.Infrastructure.Integrations.WhatsApp;

// Persistência local criptografada: montar OutboxPath em volume persistente no servidor.
public sealed class WhatsAppOutbox(IServiceScopeFactory scopes, IDataProtectionProvider protection,
    IOptions<WhatsAppSettings> options, ILogger<WhatsAppOutbox> logger) : BackgroundService, IWhatsAppQueue
{
    private readonly IDataProtector _protector = protection.CreateProtector("SyncBar.WhatsApp.Outbox.v1");
    private readonly string _directory = Path.GetFullPath(options.Value.OutboxPath);

    public async Task<Result> EnqueueAsync(string phoneNumber, string message, string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.Token) || string.IsNullOrWhiteSpace(options.Value.ConnectionId))
            return Result.Failure(new Error("WhatsApp.NotConfigured", "Configure as credenciais do WhatsApp antes de enviar."));
        try
        {
            Directory.CreateDirectory(_directory);
            var file = Path.Combine(_directory, Guid.NewGuid().ToString("N"));
            await File.WriteAllTextAsync(file + ".tmp", _protector.Protect(JsonSerializer.Serialize(new Message(phoneNumber, message, fileUrl, 0))), cancellationToken);
            File.Move(file + ".tmp", file + ".pending");
            return Result.Success();
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "Não foi possível persistir o envio WhatsApp.");
            return Result.Failure(new Error("WhatsApp.QueueUnavailable", "Não foi possível agendar o envio. Tente novamente."));
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(_directory);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            foreach (var file in Directory.EnumerateFiles(_directory, "*.pending").Take(50))
            {
                try
                {
                    var message = JsonSerializer.Deserialize<Message>(_protector.Unprotect(await File.ReadAllTextAsync(file, stoppingToken)));
                    if (message is null) throw new InvalidDataException("Mensagem inválida na fila.");
                    using var scope = scopes.CreateScope();
                    var sender = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                    var result = await sender.SendImageAsync(message.Phone, message.Text, message.Url, stoppingToken);
                    if (result.IsSuccess) File.Delete(file);
                    else if (message.Attempts >= 2)
                    {
                        File.Move(file, Path.ChangeExtension(file, ".failed"));
                        logger.LogError("Envio WhatsApp {MessageId} falhou: {Error}", Path.GetFileNameWithoutExtension(file), result.Error.Code);
                    }
                    else
                    {
                        var temporary = file + ".tmp";
                        await File.WriteAllTextAsync(temporary, _protector.Protect(JsonSerializer.Serialize(message with { Attempts = message.Attempts + 1 })), stoppingToken);
                        File.Move(temporary, file, true);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception ex) { logger.LogError(ex, "Falha ao processar item da fila WhatsApp."); }
            }
    }

    private sealed record Message(string Phone, string Text, string Url, int Attempts);
}
