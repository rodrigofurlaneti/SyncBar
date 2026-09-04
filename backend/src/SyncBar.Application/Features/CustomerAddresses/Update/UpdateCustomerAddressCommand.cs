using FluentValidation;
using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.CustomerAddresses.Update
{
    public sealed record UpdateCustomerAddressCommand(
        long Id,
        long CompanyId,
        long? BranchId,
        long? CustomerId,
        string Street,
        string Number,
        string Supplement
    ) : ICommand;
}
