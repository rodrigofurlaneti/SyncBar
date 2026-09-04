using FluentValidation;
namespace SyncBar.Application.Features.CustomerAddresses.Create
{
    public sealed class CreateCustomerAddressCommandValidator : AbstractValidator<CreateCustomerAddressCommand>
    {
        public CreateCustomerAddressCommandValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("CompanyId is required.");

            RuleFor(x => x.Street)
                .NotEmpty()
                .WithMessage("Street is required.")
                .MaximumLength(500)
                .WithMessage("Street must not exceed 500 characters.");

            RuleFor(x => x.Number)
                .NotEmpty()
                .WithMessage("Number is required.")
                .MaximumLength(50)
                .WithMessage("Number must not exceed 50 characters.");

            RuleFor(x => x.Supplement)
                .MaximumLength(50)
                .WithMessage("Supplement must not exceed 50 characters.");
        }
    }
}
