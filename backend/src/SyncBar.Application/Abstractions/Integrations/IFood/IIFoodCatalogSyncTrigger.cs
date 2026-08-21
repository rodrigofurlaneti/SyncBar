namespace SyncBar.Application.Abstractions.Integrations.IFood;

/// <summary>
/// Dispara a sincronização do cardápio com o iFood em segundo plano, sem bloquear o handler que
/// acabou de criar/editar um produto ou categoria. Implementação cria seu PRÓPRIO escopo de DI
/// (mesmo padrão do IFoodOrderPollingBackgroundService) em vez de reaproveitar o escopo da
/// requisição HTTP original — diferente do fire-and-forget de log em BaseCommandHandler, aqui a
/// sincronização faz várias chamadas HTTP pro iFood e pode demorar segundos, tempo suficiente
/// pro escopo da requisição já ter sido descartado quando a resposta HTTP volta pro cliente.
/// </summary>
public interface IIFoodCatalogSyncTrigger
{
    void TriggerCompanySync(long companyId);
}
