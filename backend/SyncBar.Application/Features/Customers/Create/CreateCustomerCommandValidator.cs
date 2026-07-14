using FluentValidation;

namespace SyncBar.Application.Features.Customers.Create;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Cpf).Length(11).When(x => x.Cpf is not null);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(150).When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
