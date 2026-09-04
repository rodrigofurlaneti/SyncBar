using FluentValidation;

namespace SyncBar.Application.Features.CustomerAppUser.Update
{
    public sealed class UpdateCustomerAppUserCommandValidator : AbstractValidator<UpdateCustomerAppUserCommand>
    {
        public UpdateCustomerAppUserCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Id is required.");

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

            When(x => !string.IsNullOrWhiteSpace(x.Password), () =>
            {
                RuleFor(x => x.Password)
                    .MinimumLength(6)
                    .WithMessage("Password must be at least 6 characters long.");
            });
        }
    }
}