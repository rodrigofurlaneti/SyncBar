using FluentValidation;
namespace SyncBar.Application.Features.CustomerAddresses.Remove
{
    public sealed class RemoveCustomerAddressCommandValidator : AbstractValidator<RemoveCustomerAddressCommand>
    {
        public RemoveCustomerAddressCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Id is required.");
        }
    }
}
