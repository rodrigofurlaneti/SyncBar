using FluentValidation;

namespace SyncBar.Application.Features.PublicOrdering.ValidateTableReading;

public sealed class ValidateTableReadingCommandValidator : AbstractValidator<ValidateTableReadingCommand>
{
    private static readonly string[] ValidMethods = ["camera", "barcode", "qrcode"];

    public ValidateTableReadingCommandValidator()
    {
        RuleFor(x => x.TableToken).NotEmpty();
        RuleFor(x => x.Method)
            .NotEmpty()
            .Must(m => ValidMethods.Contains(m, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Method must be one of: camera, barcode, qrcode.");

        When(x => string.Equals(x.Method, "camera", StringComparison.OrdinalIgnoreCase), () =>
            RuleFor(x => x.PhotoBase64).NotEmpty().WithMessage("PhotoBase64 is required for the camera method."));

        When(x => string.Equals(x.Method, "barcode", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(x.Method, "qrcode", StringComparison.OrdinalIgnoreCase), () =>
            RuleFor(x => x.ScannedValue).NotEmpty().WithMessage("ScannedValue is required for barcode/qrcode methods."));
    }
}
