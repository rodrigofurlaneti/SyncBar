using FluentValidation;
namespace SyncBar.Application.Features.Dining.Assignment.End
{
    public sealed class EndDiningAreaAssignmentCommandValidator : AbstractValidator<EndDiningAreaAssignmentCommand>
    {
        public EndDiningAreaAssignmentCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.EndAt).NotEmpty();
        }
    }
}
