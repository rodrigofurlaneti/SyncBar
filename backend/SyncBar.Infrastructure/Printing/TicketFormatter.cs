using System.Globalization;

namespace SyncBar.Infrastructure.Printing;

// Formatacao pura dos cupons (42 colunas) — testavel sem impressora.
public static class TicketFormatter
{
    public const int Width = 42;
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public sealed record OrderTicketItem(decimal Quantity, string ProductName, string? Notes, int LimitMinutes, string? RequestedBy);
    public sealed record BillItem(decimal Quantity, string ProductName, decimal UnitPrice, decimal TotalAmount);
    public sealed record PaymentLine(string Label, decimal Amount);
    public sealed record ReceiptPayment(string Label, decimal Amount, decimal? Change, string? AuthorizationCode);

    public static string OrderTicket(
        string originLabel, string? customerName, long orderId, DateTime printedAt,
        IReadOnlyCollection<OrderTicketItem> items)
    {
        var lines = new List<string>
        {
            Center("*** PEDIDO ***"),
            Center(originLabel),
            Separator(),
            $"Pedido #{orderId}  {printedAt.ToString("dd/MM/yyyy HH:mm", PtBr)}",
        };
        if (!string.IsNullOrWhiteSpace(customerName))
            lines.Add($"Cliente: {customerName}");
        lines.Add(Separator());

        foreach (var item in items)
        {
            lines.Add($"{item.Quantity:0.###} x {item.ProductName.ToUpperInvariant()}");
            if (!string.IsNullOrWhiteSpace(item.Notes))
                lines.Add($"   Obs: {item.Notes}");
            lines.Add($"   Entrega em ate {item.LimitMinutes} min");
            if (!string.IsNullOrWhiteSpace(item.RequestedBy))
                lines.Add($"   Lancado por: {item.RequestedBy}");
            lines.Add("");
        }

        lines.Add(Separator());
        return string.Join("\n", lines);
    }

    public static string Bill(
        string establishment, string originLabel, string? customerName, long orderId,
        DateTime printedAt, IReadOnlyCollection<BillItem> items,
        decimal subtotal, decimal discount, decimal serviceFee, decimal total)
    {
        var lines = new List<string>
        {
            Center(establishment.ToUpperInvariant()),
            Center("CONTA - " + originLabel),
            Separator(),
            $"Pedido #{orderId}  {printedAt.ToString("dd/MM/yyyy HH:mm", PtBr)}",
        };
        if (!string.IsNullOrWhiteSpace(customerName))
            lines.Add($"Cliente: {customerName}");
        lines.Add(Separator());

        foreach (var item in items)
            lines.Add(Row($"{item.Quantity:0.###}x {item.ProductName}", Money(item.TotalAmount)));

        lines.Add(Separator());
        lines.Add(Row("Subtotal", Money(subtotal)));
        if (discount > 0) lines.Add(Row("Desconto", "-" + Money(discount)));
        if (serviceFee > 0) lines.Add(Row("Servico (10%)", Money(serviceFee)));
        lines.Add(Row("TOTAL", Money(total)));
        lines.Add(Separator());
        lines.Add(Center("Obrigado pela preferencia!"));
        return string.Join("\n", lines);
    }

    public static string PaymentReceipt(
        string establishment, string originLabel, string? customerName,
        long saleNumber, long orderId, DateTime paidAt,
        decimal totalAmount, IReadOnlyCollection<ReceiptPayment> payments, string? operatorName)
    {
        var lines = new List<string>
        {
            Center(establishment.ToUpperInvariant()),
            Center("COMPROVANTE DE PAGAMENTO"),
            Center(originLabel),
            Separator(),
            $"Venda #{saleNumber}  Pedido #{orderId}",
            paidAt.ToString("dd/MM/yyyy HH:mm", PtBr),
        };
        if (!string.IsNullOrWhiteSpace(customerName))
            lines.Add($"Cliente: {customerName}");
        lines.Add(Separator());
        lines.Add(Row("TOTAL DA CONTA", Money(totalAmount)));
        lines.Add(Separator());

        foreach (var payment in payments)
        {
            lines.Add(Row(payment.Label, Money(payment.Amount)));
            if (payment.Change is > 0)
                lines.Add(Row("  Troco", Money(payment.Change.Value)));
            if (!string.IsNullOrWhiteSpace(payment.AuthorizationCode))
                lines.Add($"  Aut: {payment.AuthorizationCode}");
        }

        lines.Add(Separator());
        if (!string.IsNullOrWhiteSpace(operatorName))
            lines.Add($"Operador: {operatorName}");
        lines.Add(Center("*** CONTA PAGA ***"));
        return string.Join("\n", lines);
    }

    public static string CashClosing(
        string registerName, long sessionId, DateTime openedAt, DateTime? closedAt,
        string? openedBy, string? closedBy, decimal openingAmount,
        int salesCount, decimal salesTotal, IReadOnlyCollection<PaymentLine> payments,
        decimal suprimento, decimal sangria, decimal despesa,
        decimal expected, decimal counted, decimal difference)
    {
        var lines = new List<string>
        {
            Center("FECHAMENTO DE CAIXA"),
            Center(registerName),
            Separator(),
            $"Sessao #{sessionId}",
            $"Abertura : {openedAt.ToString("dd/MM HH:mm", PtBr)}  {openedBy}",
            $"Fechamento: {closedAt?.ToString("dd/MM HH:mm", PtBr)}  {closedBy}",
            Separator(),
            Row("Fundo de troco", Money(openingAmount)),
            Row($"Vendas ({salesCount})", Money(salesTotal)),
        };

        foreach (var payment in payments)
            lines.Add(Row("  " + payment.Label, Money(payment.Amount)));

        if (suprimento > 0) lines.Add(Row("Suprimentos", "+" + Money(suprimento)));
        if (sangria > 0) lines.Add(Row("Sangrias", "-" + Money(sangria)));
        if (despesa > 0) lines.Add(Row("Despesas", "-" + Money(despesa)));

        lines.Add(Separator());
        lines.Add(Row("Esperado em dinheiro", Money(expected)));
        lines.Add(Row("Contado na gaveta", Money(counted)));
        lines.Add(Separator());

        lines.Add(Center(difference == 0
            ? "*** BATEU O CAIXA ***"
            : difference > 0
                ? $"*** SOBRA DE CAIXA: {Money(difference)} ***"
                : $"*** FALTA DE CAIXA: {Money(Math.Abs(difference))} ***"));

        return string.Join("\n", lines);
    }

    private static string Money(decimal value) => "R$ " + value.ToString("N2", PtBr);
    private static string Separator() => new('-', Width);

    private static string Center(string text)
        => text.Length >= Width ? text[..Width] : text.PadLeft((Width + text.Length) / 2).PadRight(Width).TrimEnd();

    private static string Row(string left, string right)
    {
        var space = Width - right.Length;
        var label = left.Length > space - 1 ? left[..(space - 1)] : left;
        return label.PadRight(space) + right;
    }
}
