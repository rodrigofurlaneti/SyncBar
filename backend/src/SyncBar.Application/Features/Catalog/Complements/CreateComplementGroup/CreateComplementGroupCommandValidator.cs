using FluentValidation;
using SyncBar.Domain.Constants;

namespace SyncBar.Application.Features.Catalog.Complements.CreateComplementGroup;

public sealed class CreateComplementGroupCommandValidator : AbstractValidator<CreateComplementGroupCommand>
{
    private static readonly long[] ValidTypes =
    [
        ComplementGroupTypeIds.SelecaoAdicional,
        ComplementGroupTypeIds.Especificacao,
        ComplementGroupTypeIds.Ingredientes,
        ComplementGroupTypeIds.Utensilios
    ];

    public CreateComplementGroupCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ComplementGroupTypeId).Must(t => ValidTypes.Contains(t))
            .WithMessage("Invalid complement group type.");
        RuleFor(x => x.MinSelection).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxSelection).GreaterThanOrEqualTo(1);
        RuleFor(x => x).Must(x => x.MinSelection <= x.MaxSelection)
            .WithMessage("Minimum selection cannot be greater than maximum selection.");
    }
}
