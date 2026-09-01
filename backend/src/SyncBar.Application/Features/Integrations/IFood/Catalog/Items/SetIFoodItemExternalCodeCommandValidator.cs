using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Items;

public sealed class SetIfoodItemExternalCodeCommandValidator : AbstractValidator<SetIfoodItemExternalCodeCommand>
{
    public SetIfoodItemExternalCodeCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ItemId).NotEmpty();
    }
}
