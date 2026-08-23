using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Pizza;

// Fase 17 — cria/atualiza uma pizza no catálogo v1 (legado) do iFood a partir de uma
// PizzaConfiguration do SyncBar. Diferente de produto/complemento/categoria (idempotentes por id
// local gerado pelo SyncBar e enviado no create), a API de pizza do v1 NÃO aceita um id proposto —
// só devolve um na criação (ver comentário em IFoodPizzaMapping) — por isso este comando decide
// sozinho, olhando IFoodPizzaMapping, se deve chamar CreatePizza (1ª vez) ou UpdatePizza
// (mapeamento já existe pra essa PizzaConfiguration×Branch).
public sealed record SyncIFoodPizzaCommand(long BranchId, long PizzaConfigurationId) : ICommand<SyncIFoodPizzaResult>;

public sealed record SyncIFoodPizzaResult(string IFoodPizzaId);
