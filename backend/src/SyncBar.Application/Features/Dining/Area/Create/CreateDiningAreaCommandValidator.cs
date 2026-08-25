using FluentValidation;
namespace SyncBar.Application.Features.Dining.Area.Create
{
    public sealed class CreateDiningAreaCommandValidator : AbstractValidator<CreateDiningAreaCommand>
    {
        public CreateDiningAreaCommandValidator()
        {
            RuleFor(x => x.BranchId).GreaterThan(0);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }
}
