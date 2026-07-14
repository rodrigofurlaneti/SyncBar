using FluentValidation;

namespace SyncBar.Application.Features.Suppliers.Create;

public sealed class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TradeName).MaximumLength(150);
        RuleFor(x => x.Cnpj).Length(14).When(x => x.Cnpj is not null);
        RuleFor(x => x.Email).MaximumLength(150).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(20);
    }
}
