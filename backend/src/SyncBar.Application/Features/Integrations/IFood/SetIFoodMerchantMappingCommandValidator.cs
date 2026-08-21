using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood;

public sealed class SetIFoodMerchantMappingCommandValidator : AbstractValidator<SetIFoodMerchantMappingCommand>
{
    public SetIFoodMerchantMappingCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.MerchantId).MaximumLength(100);
        RuleFor(x => x.MerchantUuid).MaximumLength(100);
    }
}
