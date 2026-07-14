using FluentValidation;

namespace SyncBar.Application.Features.Branches.Create;

public sealed class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}
