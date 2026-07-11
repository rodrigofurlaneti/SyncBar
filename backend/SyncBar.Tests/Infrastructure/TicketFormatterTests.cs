using FluentAssertions;
using SyncBar.Infrastructure.Printing;
using Xunit;

namespace SyncBar.Tests.Infrastructure;

public sealed class TicketFormatterTests
{
    [Fact]
    public void OrderTicket_ShouldContainOriginItemsRequesterAndDeadline()
    {
        var content = TicketFormatter.OrderTicket(
            "MESA 9", "Joao", 14, new DateTime(2026, 7, 11, 18, 30, 0),
            [new TicketFormatter.OrderTicketItem(2, "Caipirinha de Limão", "sem açúcar", 8, "Maria")]);

        content.Should().Contain("*** PEDIDO ***");
        content.Should().Contain("MESA 9");
        content.Should().Contain("Pedido #14");
        content.Should().Contain("11/07/2026 18:30");
        content.Should().Contain("Cliente: Joao");
        content.Should().Contain("2 x CAIPIRINHA DE LIMÃO");
        content.Should().Contain("Obs: sem açúcar");
        content.Should().Contain("Entrega em ate 8 min");
        content.Should().Contain("Lancado por: Maria");
    }

    [Fact]
    public void Bill_ShouldContainTotalsAndServiceFee()
    {
        var content = TicketFormatter.Bill(
            "Restaurante Exemplo", "COMANDA 37", "Ana", 20, new DateTime(2026, 7, 11, 21, 0, 0),
            [new TicketFormatter.BillItem(1, "Picanha na Chapa", 79m, 79m)],
            subtotal: 79m, discount: 0m, serviceFee: 7.90m, total: 86.90m);

        content.Should().Contain("CONTA - COMANDA 37");
        content.Should().Contain("Cliente: Ana");
        content.Should().Contain("R$ 79,00");
        content.Should().Contain("Servico (10%)");
        content.Should().Contain("R$ 86,90");
    }

    [Theory]
    [InlineData(0, "*** BATEU O CAIXA ***")]
    [InlineData(12.5, "SOBRA DE CAIXA: R$ 12,50")]
    [InlineData(-8.0, "FALTA DE CAIXA: R$ 8,00")]
    public void CashClosing_ShouldReportDifferenceOutcome(decimal difference, string expected)
    {
        var content = TicketFormatter.CashClosing(
            "Caixa 01", 9, new DateTime(2026, 7, 11, 8, 0, 0), new DateTime(2026, 7, 11, 23, 0, 0),
            "Ana", "Gil", 100m, 12, 1500m,
            [new TicketFormatter.PaymentLine("Dinheiro", 500m), new TicketFormatter.PaymentLine("Pix", 1000m)],
            suprimento: 0m, sangria: 200m, despesa: 0m,
            expected: 400m, counted: 400m + difference, difference: difference);

        content.Should().Contain("FECHAMENTO DE CAIXA");
        content.Should().Contain("Sangrias");
        content.Should().Contain(expected);
    }

    [Fact]
    public void EscPosNormalize_ShouldStripAccents()
        => EscPos.Normalize("Porção de Batata Frita à moda").Should().Be("Porcao de Batata Frita a moda");
}
