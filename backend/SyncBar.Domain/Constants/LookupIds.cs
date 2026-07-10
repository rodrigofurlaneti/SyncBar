namespace SyncBar.Domain.Constants;

// Ids fixos dos lookups seedados em BarRestaurante_Seed.sql — nunca alterar sem alterar o seed.
public static class OrderStatusIds
{
    public const long Aberto = 1;
    public const long EmAndamento = 2;
    public const long AguardandoPagamento = 3;
    public const long Pago = 4;
    public const long Cancelado = 5;
}

public static class OrderItemStatusIds
{
    public const long Lancado = 1;
    public const long EnviadoCozinha = 2;
    public const long EmPreparo = 3;
    public const long Pronto = 4;
    public const long Entregue = 5;
    public const long Cancelado = 6;
}

public static class TableStatusIds
{
    public const long Livre = 1;
    public const long Ocupada = 2;
    public const long Reservada = 3;
    public const long EmFechamento = 4;
    public const long Interditada = 5;
}

public static class ComandaStatusIds
{
    public const long Disponivel = 1;
    public const long EmUso = 2;
    public const long Extraviada = 3;
    public const long Bloqueada = 4;
}

public static class CashSessionStatusIds
{
    public const long Aberto = 1;
    public const long Fechado = 2;
    public const long Conferido = 3;
}

public static class CashMovementTypeIds
{
    public const long Suprimento = 1;
    public const long Sangria = 2;
    public const long RecebimentoVenda = 3;
    public const long EstornoVenda = 4;
    public const long Despesa = 5;
}

public static class StockMovementTypeIds
{
    public const long EntradaCompra = 1;
    public const long SaidaVenda = 2;
    public const long AjusteEntrada = 3;
    public const long AjusteSaida = 4;
    public const long Perda = 5;
    public const long Quebra = 6;
    public const long TransferenciaEntrada = 7;
    public const long TransferenciaSaida = 8;
    public const long DevolucaoFornecedor = 9;
    public const long ConsumoInterno = 10;
}

public static class CostTypeIds
{
    public const long Fixo = 1;
    public const long Variavel = 2;
}

public static class PaymentMethodIds
{
    public const long Dinheiro = 1;
    public const long CartaoCredito = 2;
    public const long CartaoDebito = 3;
    public const long Pix = 4;
    public const long ValeRefeicao = 5;
    public const long ValeAlimentacao = 6;
    public const long Cortesia = 7;
}
