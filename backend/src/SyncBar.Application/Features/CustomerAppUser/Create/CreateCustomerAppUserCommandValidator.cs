using FluentValidation;
namespace SyncBar.Application.Features.CustomerAppUser.Create
{
    public sealed class CreateCustomerAppUserCommandValidator : AbstractValidator<CreateCustomerAppUserCommand>
    {
        public CreateCustomerAppUserCommandValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("CompanyId is required.");
            RuleFor(x => x.UserName)
                .NotEmpty()
                .WithMessage("UserName is required.")
                .MaximumLength(100)
                .WithMessage("UserName must not exceed 100 characters.");
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("A valid email address is required.")
                .MaximumLength(150)
                .WithMessage("Email must not exceed 150 characters.");
            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required.")
                .MinimumLength(6)
                .WithMessage("Password must be at least 6 characters long.");
        }
    }
}