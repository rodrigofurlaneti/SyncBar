using FluentValidation;
namespace SyncBar.Application.Features.Dining.Table.Update
{
    public sealed class UpdateDiningAreaTableCommandValidator : AbstractValidator<UpdateDiningAreaTableCommand>
    {
        public UpdateDiningAreaTableCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.DiningAreaId).GreaterThan(0);
            RuleFor(x => x.DiningTableId).GreaterThan(0);
        }
    }
}