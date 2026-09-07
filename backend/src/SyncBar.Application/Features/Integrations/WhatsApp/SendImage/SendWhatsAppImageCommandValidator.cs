using FluentValidation;

namespace SyncBar.Application.Features.Integrations.WhatsApp.SendImage
{
    public sealed class SendWhatsAppImageCommandValidator : AbstractValidator<SendWhatsAppImageCommand>
    {
        public SendWhatsAppImageCommandValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("O número de telefone é obrigatório.")
                .MaximumLength(20).WithMessage("O número de telefone deve ter no máximo 20 caracteres.");

            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("A mensagem é obrigatória.")
                .MaximumLength(1000).WithMessage("A mensagem deve ter no máximo 1000 caracteres.");

            RuleFor(x => x.FileUrl)
                .NotEmpty().WithMessage("A URL do arquivo é obrigatória.")
                .Must(BeAValidUrl).WithMessage("A URL do arquivo é inválida.");
        }

        private static bool BeAValidUrl(string fileUrl) =>
            Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
