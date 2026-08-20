using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Complements.AddComplement;

// Adiciona uma opção (Complement) a um ComplementGroup já existente — ex.: dentro do grupo
// "Escolha uma bebida", adiciona a opção "Coca-Cola" apontando pro ComplementItem "Coca-Cola"
// com ExtraPrice R$ 6,00.
public sealed record AddComplementCommand(long ComplementGroupId, long ComplementItemId, decimal ExtraPrice) : ICommand<long>;
