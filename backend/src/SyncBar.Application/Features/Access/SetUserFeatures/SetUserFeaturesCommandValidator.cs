using FluentValidation;

namespace SyncBar.Application.Features.Access.SetUserFeatures;

public sealed class SetUserFeaturesCommandValidator : AbstractValidator<SetUserFeaturesCommand>
{
    public SetUserFeaturesCommandValidator()
    {
        RuleFor(x => x.AppUserId).GreaterThan(0);
        RuleFor(x => x.FeatureIds).NotNull();
    }
}
