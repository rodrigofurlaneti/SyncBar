namespace SyncBar.Application.Abstractions.Integrations.Ifood;

/// <summary>
/// Dispara a sincronização do cardápio com o Ifood em segundo plano, sem bloquear o handler que
/// acabou de criar/editar um produto ou categoria. Implementação cria seu PRÓPRIO escopo de DI
/// (mesmo padrão do IfoodOrderPollingBackgroundService) em vez de reaproveitar o escopo da
/// requisição HTTP original — diferente do fire-and-forget de log em BaseCommandHandler, aqui a
/// sincronização faz várias chamadas HTTP pro Ifood e pode demorar segundos, tempo suficiente
/// pro escopo da requisição já ter sido descartado quando a resposta HTTP volta pro cliente.
/// </summary>
public interface IIfoodCatalogSyncTrigger
{
    void TriggerCompanySync(long companyId);
}
