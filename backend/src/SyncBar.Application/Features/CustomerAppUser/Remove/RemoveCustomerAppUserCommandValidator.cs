using FluentValidation;
namespace SyncBar.Application.Features.CustomerAppUser.Remove
{
    public sealed class RemoveCustomerAppUserCommandValidator : AbstractValidator<RemoveCustomerAppUserCommand>
    {
        public RemoveCustomerAppUserCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Id is required.");
        }
    }
}
