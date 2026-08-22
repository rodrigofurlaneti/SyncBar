using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Admin;

public sealed class UploadIFoodImageCommandValidator : AbstractValidator<UploadIFoodImageCommand>
{
    public UploadIFoodImageCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.JsonBody).NotEmpty();
    }
}
