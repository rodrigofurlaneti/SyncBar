using FluentValidation;
namespace SyncBar.Application.Features.Dining.Area.Update
{
    public sealed class UpdateDiningAreaCommandValidator : AbstractValidator<UpdateDiningAreaCommand>
    {
        public UpdateDiningAreaCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }
}
