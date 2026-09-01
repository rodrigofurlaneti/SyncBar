using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood;

public sealed class TestIfoodConnectionCommandValidator : AbstractValidator<TestIfoodConnectionCommand>
{
    public TestIfoodConnectionCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
    }
}
