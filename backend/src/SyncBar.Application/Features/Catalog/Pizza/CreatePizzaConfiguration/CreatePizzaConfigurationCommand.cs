using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Pizza.CreatePizzaConfiguration;

// Fase 17 — torna um Product existente "configurável como pizza" (1:1). Get-or-create: se o
// produto já tem uma configuração ativa, retorna o id dela em vez de duplicar (mesmo padrão de
// get-or-create documentado em IfoodProductMapping/ProductComplementGroup pra contornar a
// ausência de índice único filtrado no MySQL).
public sealed record CreatePizzaConfigurationCommand(long ProductId) : ICommand<long>;
