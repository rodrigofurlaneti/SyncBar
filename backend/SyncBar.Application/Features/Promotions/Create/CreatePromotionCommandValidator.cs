using FluentValidation;

namespace SyncBar.Application.Features.Promotions.Create;

public sealed class CreatePromotionCommandValidator : AbstractValidator<CreatePromotionCommand>
{
    public CreatePromotionCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DayOfWeek).InclusiveBetween(0, 6);
        RuleFor(x => x.StartMinuteOfDay).InclusiveBetween(0, 1439);
        RuleFor(x => x.EndMinuteOfDay).InclusiveBetween(1, 1440);
        RuleFor(x => x).Must(x => x.StartMinuteOfDay < x.EndMinuteOfDay)
            .WithMessage("Horário inicial deve ser antes do final.");
        RuleFor(x => x.PromotionTypeId).InclusiveBetween(1, 2);
        RuleFor(x => x.DiscountRate)
            .NotNull().WithMessage("Informe o percentual de desconto.")
            .ExclusiveBetween(0m, 1m).WithMessage("Desconto deve estar entre 0% e 100%.")
            .When(x => x.PromotionTypeId == 2);
    }
}
