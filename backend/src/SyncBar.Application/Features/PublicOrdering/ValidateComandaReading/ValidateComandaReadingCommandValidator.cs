using FluentValidation;

namespace SyncBar.Application.Features.PublicOrdering.ValidateComandaReading;

public sealed class ValidateComandaReadingCommandValidator : AbstractValidator<ValidateComandaReadingCommand>
{
    private static readonly string[] ValidMethods = ["camera", "barcode", "qrcode"];

    public ValidateComandaReadingCommandValidator()
    {
        RuleFor(x => x.TableToken).NotEmpty();
        RuleFor(x => x.ComandaCode).NotEmpty();
        RuleFor(x => x.Method)
            .NotEmpty()
            .Must(m => ValidMethods.Contains(m.ToLowerInvariant()))
            .WithMessage("Method must be one of: camera, barcode, qrcode.");

        When(x => x.Method.ToLowerInvariant() == "camera", () =>
            RuleFor(x => x.PhotoBase64).NotEmpty().WithMessage("PhotoBase64 is required for the camera method."));

        When(x => x.Method.ToLowerInvariant() is "barcode" or "qrcode", () =>
            RuleFor(x => x.ScannedValue).NotEmpty().WithMessage("ScannedValue is required for barcode/qrcode methods."));
    }
}
