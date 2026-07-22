using FluentValidation;

namespace SyncBar.Application.Features.Catalog.SetProductImage;

public sealed class SetProductImageCommandValidator : AbstractValidator<SetProductImageCommand>
{
    private const int MaxBytes = 2 * 1024 * 1024; // 2 MB
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public SetProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Extension)
            .Must(e => AllowedExtensions.Contains(e.ToLowerInvariant()))
            .WithMessage("Formato inválido — use JPG, PNG ou WebP.");
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Arquivo vazio.")
            .Must(c => c.Length <= MaxBytes).WithMessage("Imagem maior que 2 MB.");
    }
}
