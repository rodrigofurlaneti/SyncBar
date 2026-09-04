using FluentValidation;
namespace SyncBar.Application.Features.CustomerAddresses.RegisterOrder
{
    public sealed class RegisterCustomerAddressOrderCommandValidator : AbstractValidator<RegisterCustomerAddressOrderCommand>
    {
        public RegisterCustomerAddressOrderCommandValidator()
        {
            RuleFor(x => x.AddressId)
                .GreaterThan(0)
                .WithMessage("AddressId is required.");

            RuleFor(x => x.OrderId)
                .GreaterThan(0)
                .WithMessage("OrderId is required.");
        }
    }
}
