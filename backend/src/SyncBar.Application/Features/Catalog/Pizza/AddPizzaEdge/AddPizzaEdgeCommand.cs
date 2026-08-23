using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Pizza.AddPizzaEdge;

public sealed record AddPizzaEdgeCommand(
    long PizzaConfigurationId,
    string Name,
    decimal ExtraPrice,
    int DisplayOrder) : ICommand<long>;
