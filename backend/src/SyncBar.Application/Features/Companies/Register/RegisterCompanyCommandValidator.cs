using FluentValidation;

namespace SyncBar.Application.Features.Companies.Register;

public sealed class RegisterCompanyCommandValidator : AbstractValidator<RegisterCompanyCommand>
{
    public RegisterCompanyCommandValidator()
    {
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TradeName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Cnpj).NotEmpty().Length(14);
        RuleFor(x => x.CompanyEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.CompanyEmail));
        RuleFor(x => x.BranchName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.AdminUserName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.AdminPassword).NotEmpty().MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.");
    }
}
