using FluentValidation;

namespace SyncBar.Application.Features.Printing.CreatePrinter;

public sealed class CreatePrinterCommandValidator : AbstractValidator<CreatePrinterCommand>
{
    public CreatePrinterCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ConnectionType).InclusiveBetween(1, 2);
        RuleFor(x => x.PrinterName).NotEmpty().MaximumLength(200)
            .When(x => x.ConnectionType == 1)
            .WithMessage("Informe o nome exato do driver instalado no Windows.");
        RuleFor(x => x.IpAddress).NotEmpty().MaximumLength(45)
            .When(x => x.ConnectionType == 2)
            .WithMessage("Informe o IP da impressora de rede.");
        RuleFor(x => x.Port).InclusiveBetween(1, 65535)
            .When(x => x.ConnectionType == 2)
            .WithMessage("Porta inválida (padrão: 9100).");
        RuleFor(x => x).Must(x => x.PrintsOrders || x.PrintsBills)
            .WithMessage("A impressora precisa imprimir pedidos, contas ou ambos.");
    }
}
