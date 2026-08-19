using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood;

public sealed class TestIFoodConnectionCommandValidator : AbstractValidator<TestIFoodConnectionCommand>
{
    public TestIFoodConnectionCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
    }
}
