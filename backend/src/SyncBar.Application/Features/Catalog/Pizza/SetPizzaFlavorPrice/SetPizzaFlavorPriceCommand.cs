using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Pizza.SetPizzaFlavorPrice;

// Upsert (ver PizzaConfiguration.SetFlavorPrice): a EXISTÊNCIA desta linha é o que torna o sabor
// vendável naquele tamanho, ver comentário em PizzaFlavorPrice.
public sealed record SetPizzaFlavorPriceCommand(
    long PizzaConfigurationId,
    long PizzaFlavorId,
    long PizzaSizeId,
    decimal Price) : ICommand<long>;
