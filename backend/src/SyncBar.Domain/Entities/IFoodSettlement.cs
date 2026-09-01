using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Fase 4 (financeiro) — um registro por título retornado pela API Settlement do Ifood: o
// repasse consolidado semanal (apuração segunda-domingo, consolidada toda segunda-feira) —
// REPASSE (transferência normal), BOLETO (saldo devedor), REGISTRO_RECEBIVEIS (antecipação
// registrada) ou RENEGOCIADA. Por BranchId, mesmo padrão de IfoodFinancialEvent.
public sealed class IfoodSettlement : AggregateRoot
{
    public long BranchId { get; private set; }
    public string IfoodSettlementId { get; private set; } = null!;
    public string Type { get; private set; } = null!;
    public string? Product { get; private set; }
    public decimal Amount { get; private set; }
    public string Status { get; private set; } = null!;
    public DateTime? PaymentDate { get; private set; }
    // Dados bancários só vêm preenchidos quando Status == "SUCCEED" (conforme a doc oficial).
    public string? BankCode { get; private set; }
    public string? BankAgency { get; private set; }
    public string? BankAccount { get; private set; }
    public string RawPayload { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IfoodSettlement() : base(0) { }

    private IfoodSettlement(
        long branchId, string ifoodSettlementId, string type, string? product, decimal amount,
        string status, DateTime? paymentDate, string? bankCode, string? bankAgency, string? bankAccount,
        string rawPayload)
        : base(0)
    {
        BranchId = branchId;
        IfoodSettlementId = ifoodSettlementId;
        Type = type;
        Product = product;
        Amount = amount;
        Status = status;
        PaymentDate = paymentDate;
        BankCode = bankCode;
        BankAgency = bankAgency;
        BankAccount = bankAccount;
        RawPayload = rawPayload;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IfoodSettlement> Create(
        long branchId, string IfoodSettlementId, string type, string? product, decimal amount,
        string status, DateTime? paymentDate, string? bankCode, string? bankAgency, string? bankAccount,
        string rawPayload)
    {
        if (string.IsNullOrWhiteSpace(IfoodSettlementId))
            return Result.Failure<IfoodSettlement>(
                new Error("IfoodSettlement.MissingSettlementId", "Settlement requires an Ifood settlement id."));

        return Result.Success(new IfoodSettlement(
            branchId, IfoodSettlementId, type, product, amount, status, paymentDate,
            bankCode, bankAgency, bankAccount, rawPayload));
    }

    // Status/dados bancários mudam conforme o Ifood processa o título (ex.: PENDING → SUCCEED)
    // — reflete uma atualização de sincronização sem recriar o registro.
    public void UpdateFromSync(string status, DateTime? paymentDate, string? bankCode, string? bankAgency, string? bankAccount, string rawPayload)
    {
        Status = status;
        PaymentDate = paymentDate;
        BankCode = bankCode;
        BankAgency = bankAgency;
        BankAccount = bankAccount;
        RawPayload = rawPayload;
        UpdatedAt = DateTime.Now;
    }
}
