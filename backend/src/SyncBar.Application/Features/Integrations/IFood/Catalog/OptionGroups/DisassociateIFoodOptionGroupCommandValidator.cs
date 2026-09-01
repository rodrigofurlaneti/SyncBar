using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

public sealed class DisassociateIfoodOptionGroupCommandValidator : AbstractValidator<DisassociateIfoodOptionGroupCommand>
{
    public DisassociateIfoodOptionGroupCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.OptionGroupId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
