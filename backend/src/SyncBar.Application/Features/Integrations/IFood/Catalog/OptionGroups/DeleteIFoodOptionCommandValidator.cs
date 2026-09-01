using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

public sealed class DeleteIfoodOptionCommandValidator : AbstractValidator<DeleteIfoodOptionCommand>
{
    public DeleteIfoodOptionCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.OptionGroupId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
