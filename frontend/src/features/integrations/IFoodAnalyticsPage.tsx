import { useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getIFoodOrderKpis } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { Button } from "../../ui/Button";
import { TextField } from "../../ui/Field";
import { QueryError } from "../../components/QueryError";
import { EmptyState } from "../../ui/EmptyState";

// Fase 9 — Indicadores: analytics/v1.0, 1 endpoint (KPIs de pedidos). O DSL real de
// filtro/agregação é muito maior do que o exposto aqui (ver ressalva no backend,
// IIFoodAnalyticsClient) — esta tela manda um payload padrão (GMV + taxas, agrupado por canal de
// venda) e mostra o JSON bruto de cada bucket devolvido, já que a doc oficial não documenta o
// schema de resposta campo-a-campo.
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

export function IFoodAnalyticsPage() {
  const { branchId } = useAuthStore();
  const today = new Date();
  const monthAgo = new Date(today);
  monthAgo.setDate(monthAgo.getDate() - 30);

  const [periodStart, setPeriodStart] = useState(toInputDate(monthAgo));
  const [periodEnd, setPeriodEnd] = useState(toInputDate(today));
  const [submitted, setSubmitted] = useState({ periodStart: toInputDate(monthAgo), periodEnd: toInputDate(today) });

  const kpisQuery = useQuery({
    queryKey: ["integrations", "ifood", "analytics", "order-kpis", branchId, submitted.periodStart, submitted.periodEnd],
    queryFn: () => getIFoodOrderKpis(branchId, { periodStart: submitted.periodStart, periodEnd: submitted.periodEnd }),
  });

  const buckets = kpisQuery.data?.buckets ?? [];

  return (
    <main style={{ padding: 22, maxWidth: 1000, margin: "0 auto" }}>
      <div className="rise" style={{ marginBottom: 18 }}>
        <Link to="/integracoes/ifood" style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
          ← Integração iFood
        </Link>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>
          Indicadores iFood
        </h2>
        <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
          GMV, taxas e outras métricas de pedidos, agrupadas por canal de venda no período
        </span>
      </div>

      <div className="ui-row ui-row-wrap" style={{ gap: 12, alignItems: "flex-end", marginBottom: 18 }}>
        <TextField label="De" type="date" value={periodStart} onChange={(e) => setPeriodStart(e.target.value)} />
        <TextField label="Até" type="date" value={periodEnd} onChange={(e) => setPeriodEnd(e.target.value)} />
        <Button variant="primary" onClick={() => setSubmitted({ periodStart, periodEnd })}>
          Consultar
        </Button>
      </div>

      {kpisQuery.isError && <QueryError error={kpisQuery.error} what="os indicadores do iFood" />}

      {!kpisQuery.isLoading && buckets.length === 0 && !kpisQuery.isError && (
        <EmptyState title="Sem dados no período" description="Nenhum bucket de métricas foi devolvido pelo iFood pra esse intervalo." />
      )}

      <div style={{ display: "grid", gap: 10 }}>
        {buckets.map((raw, index) => (
          <pre
            key={index}
            className="card"
            style={{ padding: 12, fontSize: "0.8rem", overflowX: "auto", margin: 0, whiteSpace: "pre-wrap" }}
          >
            {prettyJson(raw)}
          </pre>
        ))}
      </div>
    </main>
  );
}
