using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.CustomerAddresses.Create
{
    public sealed record CreateCustomerAddressCommand(
        long CompanyId,
        long? BranchId,
        long? CustomerId,
        string Street,
        string Number,
        string Supplement
    ) : ICommand<long>;
}
