using FluentValidation;

namespace SyncBar.Application.Features.Access.SetJobTitleFeatures;

public sealed class SetJobTitleFeaturesCommandValidator : AbstractValidator<SetJobTitleFeaturesCommand>
{
    public SetJobTitleFeaturesCommandValidator()
    {
        RuleFor(x => x.JobTitleId).GreaterThan(0);
        RuleFor(x => x.FeatureIds).NotNull();
    }
}
