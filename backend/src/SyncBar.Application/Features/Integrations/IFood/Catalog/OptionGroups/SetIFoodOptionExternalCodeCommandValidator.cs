using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

public sealed class SetIfoodOptionExternalCodeCommandValidator : AbstractValidator<SetIfoodOptionExternalCodeCommand>
{
    public SetIfoodOptionExternalCodeCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.OptionId).NotEmpty();
        RuleFor(x => x.ExternalCode).NotEmpty();
    }
}
