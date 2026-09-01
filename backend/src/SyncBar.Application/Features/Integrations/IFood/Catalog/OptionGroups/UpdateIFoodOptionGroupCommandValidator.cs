using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

public sealed class UpdateIfoodOptionGroupCommandValidator : AbstractValidator<UpdateIfoodOptionGroupCommand>
{
    public UpdateIfoodOptionGroupCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.OptionGroupId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
