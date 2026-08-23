using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Pizza.CreatePizzaFlavor;

// Fase 17 — cadastro de sabor de pizza, reaproveitável entre várias PizzaConfiguration da mesma
// empresa (ver comentário em PizzaFlavor). Mesmo formato de CreateComplementItemCommand.
public sealed record CreatePizzaFlavorCommand(long CompanyId, string Name, string? Description) : ICommand<long>;
