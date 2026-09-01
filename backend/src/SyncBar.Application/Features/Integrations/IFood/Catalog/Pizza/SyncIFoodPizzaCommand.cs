using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Pizza;

// Fase 17 — cria/atualiza uma pizza no catálogo v1 (legado) do Ifood a partir de uma
// PizzaConfiguration do SyncBar. Diferente de produto/complemento/categoria (idempotentes por id
// local gerado pelo SyncBar e enviado no create), a API de pizza do v1 NÃO aceita um id proposto —
// só devolve um na criação (ver comentário em IfoodPizzaMapping) — por isso este comando decide
// sozinho, olhando IfoodPizzaMapping, se deve chamar CreatePizza (1ª vez) ou UpdatePizza
// (mapeamento já existe pra essa PizzaConfiguration×Branch).
public sealed record SyncIfoodPizzaCommand(long BranchId, long PizzaConfigurationId) : ICommand<SyncIfoodPizzaResult>;

public sealed record SyncIfoodPizzaResult(string IfoodPizzaId);
