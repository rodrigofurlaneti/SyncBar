using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

public sealed class UpdateIfoodOptionGroupStatusCommandValidator : AbstractValidator<UpdateIfoodOptionGroupStatusCommand>
{
    public UpdateIfoodOptionGroupStatusCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.OptionGroupId).NotEmpty();
    }
}
