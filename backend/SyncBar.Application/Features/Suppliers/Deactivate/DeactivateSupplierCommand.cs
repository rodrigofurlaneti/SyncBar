using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Suppliers.Deactivate;

public sealed record DeactivateSupplierCommand(long SupplierId) : ICommand;
