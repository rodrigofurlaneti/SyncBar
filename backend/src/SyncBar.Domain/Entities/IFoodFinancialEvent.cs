using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Fase 4 (financeiro) — um registro por evento retornado pela API Financial Events do iFood:
// o detalhamento por pedido/lançamento do que rendeu, taxas, comissão, subsídio etc. Por
// BranchId (o financeiro do iFood é por loja/merchant, igual pedidos e cardápio). Este módulo é
// só auditoria/reconciliação — não mexe no fluxo operacional (Sale/CashSession/CashMovement
// continuam sendo a fonte de verdade do caixa físico da loja).
//
// HasTransferImpact é o campo-chave: separa lançamentos que afetam o repasse (somados no
// alerta de discrepância contra IFoodSettlement) dos que são só informativos (ex.: pagamento
// recebido direto pela loja via dinheiro/vale-refeição, que não passa pelo repasse do iFood).
public sealed class IFoodFinancialEvent : AggregateRoot
{
    public long BranchId { get; private set; }
    public string IFoodEventId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? Trigger { get; private set; }
    public decimal Amount { get; private set; }
    public bool HasTransferImpact { get; private set; }
    public DateTime CompetenceDate { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public DateTime? SettlementExpectedDate { get; private set; }
    // reference.type/reference.id do payload — quando type == "ORDER", casa com
    // IFoodOrder.IFoodOrderId (entidade da Fase 2) pra mostrar o líquido recebido no pedido.
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }
    public string RawPayload { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IFoodFinancialEvent() : base(0) { }

    private IFoodFinancialEvent(
        long branchId, string ifoodEventId, string name, string? description, string? trigger,
        decimal amount, bool hasTransferImpact, DateTime competenceDate, DateTime periodStart,
        DateTime periodEnd, DateTime? settlementExpectedDate, string? referenceType, string? referenceId,
        string rawPayload)
        : base(0)
    {
        BranchId = branchId;
        IFoodEventId = ifoodEventId;
        Name = name;
        Description = description;
        Trigger = trigger;
        Amount = amount;
        HasTransferImpact = hasTransferImpact;
        CompetenceDate = competenceDate;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        SettlementExpectedDate = settlementExpectedDate;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        RawPayload = rawPayload;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IFoodFinancialEvent> Create(
        long branchId, string ifoodEventId, string name, string? description, string? trigger,
        decimal amount, bool hasTransferImpact, DateTime competenceDate, DateTime periodStart,
        DateTime periodEnd, DateTime? settlementExpectedDate, string? referenceType, string? referenceId,
        string rawPayload)
    {
        if (string.IsNullOrWhiteSpace(ifoodEventId))
            return Result.Failure<IFoodFinancialEvent>(
                new Error("IFoodFinancialEvent.MissingEventId", "Financial event requires an iFood event id."));

        return Result.Success(new IFoodFinancialEvent(
            branchId, ifoodEventId, name, description, trigger, amount, hasTransferImpact,
            competenceDate, periodStart, periodEnd, settlementExpectedDate, referenceType, referenceId, rawPayload));
    }
}
