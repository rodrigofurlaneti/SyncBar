using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood;

public sealed class SetIfoodMerchantMappingCommandValidator : AbstractValidator<SetIfoodMerchantMappingCommand>
{
    public SetIfoodMerchantMappingCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.MerchantId).MaximumLength(100);
        RuleFor(x => x.MerchantUuid).MaximumLength(100);
    }
}
