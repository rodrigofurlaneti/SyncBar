import { useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import {
  IFOOD_FINANCIAL_REPORT_TYPES,
  getIFoodFinancialReport,
  getIFoodReconciliationOnDemandStatus,
  requestIFoodReconciliationOnDemand,
  type IFoodFinancialReportType,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { useToast } from "../../ui/Toast";
import { Button } from "../../ui/Button";
import { SelectField, TextField } from "../../ui/Field";
import { QueryError } from "../../components/QueryError";
import { EmptyState } from "../../ui/EmptyState";

// Fase 9 — Relatórios financeiros: catálogo genérico dos 13 relatórios restantes do módulo
// Financial (financial/v2.0 ×12 + financial/v2.1 ×1) + anticipations/sales (financial/v3.0).
// A doc oficial não documenta o schema de resposta campo-a-campo pra estes relatórios, então
// esta tela mostra o JSON bruto de cada registro (ver comentário no backend,
// IIFoodFinancialClient) — é uma tela de auditoria/exportação, não um dashboard tratado.
const REPORT_LABELS: Record<IFoodFinancialReportType, string> = {
  SalesAdjustments: "Ajustes de vendas",
  Payments: "Pagamentos",
  PaymentDetails: "Detalhes de pagamento",
  Occurrences: "Ocorrências",
  MaintenanceFees: "Taxas de manutenção",
  IncomeTaxes: "Impostos de renda",
  Periods: "Períodos de apuração",
  ChargeCancellations: "Cancelamentos de cobrança",
  Cancellations: "Cancelamentos",
  ReceivableRecords: "Registros de recebíveis",
  SalesBenefits: "Benefícios em vendas",
  AdjustmentsBenefits: "Benefícios em ajustes",
  SalesV21: "Vendas (v2.1)",
  AnticipationsV3: "Antecipações",
  SalesV3: "Vendas (v3.0)",
};

function toInputDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

function prettyJson(raw: string): string {
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

export function IFoodFinancialReportsPage() {
  const { branchId } = useAuthStore();
  const toast = useToast();
  const today = new Date();
  const monthAgo = new Date(today);
  monthAgo.setDate(monthAgo.getDate() - 30);

  const [reportType, setReportType] = useState<IFoodFinancialReportType>("SalesAdjustments");
  const [periodId, setPeriodId] = useState("");
  const [rangeStart, setRangeStart] = useState(toInputDate(monthAgo));
  const [rangeEnd, setRangeEnd] = useState(toInputDate(today));
  const [submitted, setSubmitted] = useState<{ reportType: IFoodFinancialReportType; periodId: string; rangeStart: string; rangeEnd: string } | null>(
    null,
  );

  const [competence, setCompetence] = useState(toInputDate(today).slice(0, 7));
  const [requestId, setRequestId] = useState("");

  const reportQuery = useQuery({
    queryKey: ["integrations", "ifood", "financial", "report", branchId, submitted],
    queryFn: () =>
      getIFoodFinancialReport(branchId, submitted!.reportType, {
        periodId: submitted!.periodId || undefined,
        rangeStart: submitted!.rangeStart || undefined,
        rangeEnd: submitted!.rangeEnd || undefined,
      }),
    enabled: !!submitted,
  });

  const requestOnDemandMutation = useMutation({
    mutationFn: () => requestIFoodReconciliationOnDemand(branchId, competence),
    onSuccess: (data) => {
      setRequestId(data.requestId);
      toast.success(`Apuração solicitada. RequestId: ${data.requestId || "(veja o payload bruto)"}`);
    },
    onError: (error: unknown) => toast.error(error instanceof Error ? error.message : "Falha ao solicitar apuração."),
  });

  const statusQuery = useQuery({
    queryKey: ["integrations", "ifood", "financial", "reconciliation-status", branchId, requestId],
    queryFn: () => getIFoodReconciliationOnDemandStatus(branchId, requestId),
    enabled: false,
  });

  const items = reportQuery.data?.items ?? [];

  return (
    <main style={{ padding: 22, maxWidth: 1000, margin: "0 auto" }}>
      <div className="rise" style={{ marginBottom: 18 }}>
        <Link to="/integracoes/ifood" style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
          ← Integração iFood
        </Link>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>
          Relatórios financeiros iFood
        </h2>
        <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
          consulta direta aos relatórios do módulo Financial (v2.0/v2.1/v3.0) que não têm tela dedicada
        </span>
      </div>

      <div className="card" style={{ padding: 16, marginBottom: 18, display: "grid", gap: 12 }}>
        <SelectField
          label="Relatório"
          value={reportType}
          onChange={(e) => setReportType(e.target.value as IFoodFinancialReportType)}
        >
          {IFOOD_FINANCIAL_REPORT_TYPES.map((type) => (
            <option key={type} value={type}>
              {REPORT_LABELS[type]}
            </option>
          ))}
        </SelectField>
        <div className="ui-row ui-row-wrap" style={{ gap: 12, alignItems: "flex-end" }}>
          <TextField label="Período/PeriodId (opcional)" value={periodId} onChange={(e) => setPeriodId(e.target.value)} />
          <TextField label="De" type="date" value={rangeStart} onChange={(e) => setRangeStart(e.target.value)} />
          <TextField label="Até" type="date" value={rangeEnd} onChange={(e) => setRangeEnd(e.target.value)} />
          <Button variant="primary" onClick={() => setSubmitted({ reportType, periodId, rangeStart, rangeEnd })}>
            Consultar
          </Button>
        </div>
      </div>

      {reportQuery.isError && <QueryError error={reportQuery.error} what="o relatório financeiro" />}

      {submitted && !reportQuery.isLoading && items.length === 0 && !reportQuery.isError && (
        <EmptyState title="Sem registros" description="Nenhum registro devolvido pelo iFood pros filtros informados." />
      )}

      <div style={{ display: "grid", gap: 10, marginBottom: 24 }}>
        {items.map((raw, index) => (
          <pre
            key={index}
            className="card"
            style={{ padding: 12, fontSize: "0.8rem", overflowX: "auto", margin: 0, whiteSpace: "pre-wrap" }}
          >
            {prettyJson(raw)}
          </pre>
        ))}
      </div>

      <div className="card" style={{ padding: 16, display: "grid", gap: 12 }}>
        <strong>Apuração sob demanda (reconciliation on-demand)</strong>
        <span style={{ fontSize: "0.85rem", color: "var(--ink-faint)" }}>
          use quando a apuração automática do período ainda não foi gerada pelo iFood.
        </span>
        <div className="ui-row ui-row-wrap" style={{ gap: 12, alignItems: "flex-end" }}>
          <TextField label="Competência (yyyy-MM)" value={competence} onChange={(e) => setCompetence(e.target.value)} />
          <Button variant="primary" disabled={requestOnDemandMutation.isPending} onClick={() => requestOnDemandMutation.mutate()}>
            Solicitar apuração
          </Button>
          {requestId && (
            <Button variant="ghost" onClick={() => void statusQuery.refetch()}>
              Consultar status
            </Button>
          )}
        </div>
        {statusQuery.data?.rawPayload && (
          <pre style={{ fontSize: "0.8rem", whiteSpace: "pre-wrap", margin: 0 }}>{prettyJson(statusQuery.data.rawPayload)}</pre>
        )}
      </div>
    </main>
  );
}
