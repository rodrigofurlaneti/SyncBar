using System.Globalization;

namespace SyncBar.Infrastructure.Printing;

// Formatacao pura dos cupons (42 colunas) — testavel sem impressora.
public static class TicketFormatter
{
    public const int Width = 42;
    public const int BigWidth = 21; // linhas Big tem largura dupla → metade das colunas
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    // Marcadores de tamanho interpretados pelo EscPos (linha a linha).
    private static string Tall(string line) => EscPos.TallMarker + line;
    private static string Big(string line) => EscPos.BigMarker + line;

    private static string CenterBig(string text)
        => Big(text.Length >= BigWidth
            ? text[..BigWidth]
            : text.PadLeft((BigWidth + text.Length) / 2).TrimEnd().PadLeft(0));

    private static string RowBig(string left, string right)
    {
        var space = BigWidth - right.Length;
        var label = left.Length > space - 1 ? left[..Math.Max(0, space - 1)] : left;
        return Big(label.PadRight(Math.Max(label.Length + 1, space)) + right);
    }

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
            CenterBig(originLabel),
            Separator(),
            $"Pedido #{orderId}  {printedAt.ToString("dd/MM/yyyy HH:mm", PtBr)}",
        };
        if (!string.IsNullOrWhiteSpace(customerName))
            lines.Add(Tall($"Cliente: {customerName}"));
        lines.Add(Separator());

        foreach (var item in items)
        {
            lines.Add(Tall($"{item.Quantity:0.###} x {item.ProductName.ToUpperInvariant()}"));
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
        // Cliente le esta via: corpo inteiro em altura dupla, TOTAL gigante.
        var lines = new List<string>
        {
            CenterBig(establishment.ToUpperInvariant()),
            CenterBig("CONTA " + originLabel),
            Tall(Separator()),
            Tall($"Pedido #{orderId}  {printedAt.ToString("dd/MM/yyyy HH:mm", PtBr)}"),
        };
        if (!string.IsNullOrWhiteSpace(customerName))
            lines.Add(Tall($"Cliente: {customerName}"));
        lines.Add(Tall(Separator()));

        foreach (var item in items)
            lines.Add(Tall(Row($"{item.Quantity:0.###}x {item.ProductName}", Money(item.TotalAmount))));

        lines.Add(Tall(Separator()));
        lines.Add(Tall(Row("Subtotal", Money(subtotal))));
        if (discount > 0) lines.Add(Tall(Row("Desconto", "-" + Money(discount))));
        if (serviceFee > 0) lines.Add(Tall(Row("Servico (10%)", Money(serviceFee))));
        lines.Add(RowBig("TOTAL", Money(total)));
        lines.Add(Tall(Separator()));
        lines.Add(Tall(Center("Obrigado pela preferencia!")));
        return string.Join("\n", lines);
    }

    public static string PaymentReceipt(
        string establishment, string originLabel, string? customerName,
        long saleNumber, long orderId, DateTime paidAt,
        decimal totalAmount, IReadOnlyCollection<ReceiptPayment> payments, string? operatorName,
        decimal previouslyPaid = 0)
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
        lines.Add(Tall(Row("TOTAL DA CONTA", Money(totalAmount))));
        if (previouslyPaid > 0)
        {
            lines.Add(Tall(Row("Pago parcial (anterior)", "-" + Money(previouslyPaid))));
            lines.Add(Tall(Row("Restante quitado agora", Money(totalAmount - previouslyPaid))));
        }
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
        lines.Add(CenterBig("* CONTA PAGA *"));
        return string.Join("\n", lines);
    }

    public static string PartialPaymentReceipt(
        string establishment, string originLabel, string? payerName,
        long orderId, DateTime paidAt, string paymentMethod, decimal amount,
        string? authorizationCode, decimal orderTotal, decimal remainingAfter, string? operatorName)
    {
        var lines = new List<string>
        {
            Center(establishment.ToUpperInvariant()),
            Center("PAGAMENTO PARCIAL"),
            Center(originLabel),
            Separator(),
            $"Pedido #{orderId}  {paidAt.ToString("dd/MM/yyyy HH:mm", PtBr)}",
        };
        if (!string.IsNullOrWhiteSpace(payerName))
            lines.Add($"Pago por: {payerName}");
        lines.Add(Separator());
        lines.Add(RowBig("VALOR PAGO", Money(amount)));
        lines.Add(Row("  " + paymentMethod, Money(amount)));
        if (!string.IsNullOrWhiteSpace(authorizationCode))
            lines.Add($"  Aut: {authorizationCode}");
        lines.Add(Separator());
        lines.Add(Tall(Row("Total da conta", Money(orderTotal))));
        lines.Add(Tall(Row("Restante em aberto", Money(remainingAfter))));
        lines.Add(Separator());
        if (!string.IsNullOrWhiteSpace(operatorName))
            lines.Add($"Operador: {operatorName}");
        lines.Add(Center("*** MESA CONTINUA ABERTA ***"));
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

        var verdict = difference == 0
            ? "*** BATEU O CAIXA ***"
            : difference > 0
                ? $"*** SOBRA DE CAIXA: {Money(difference)} ***"
                : $"*** FALTA DE CAIXA: {Money(Math.Abs(difference))} ***";
        lines.Add(verdict.Length <= BigWidth ? CenterBig(verdict) : Tall(Center(verdict)));

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
