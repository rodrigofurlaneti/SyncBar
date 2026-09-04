using FluentValidation;
namespace SyncBar.Application.Features.CustomerAddresses.Update
{
    public sealed class UpdateCustomerAddressCommandValidator : AbstractValidator<UpdateCustomerAddressCommand>
    {
        public UpdateCustomerAddressCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Id is required.");

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

            RuleFor(x => x.ZipCode)
                .MaximumLength(9)
                .WithMessage("ZipCode must not exceed 9 characters.");  
        }
    }
}
