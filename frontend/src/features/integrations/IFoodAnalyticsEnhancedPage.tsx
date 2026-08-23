import { useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getIFoodOrderKpis } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { PageHeader } from "../../components/PageHeader";
import { QueryError } from "../../components/QueryError";
import { EmptyState } from "../../ui/EmptyState";
import { Button } from "../../ui/Button";
import { DashboardCard } from "../../components/DashboardCard";
import { TextField } from "../../ui/Field";

interface KpiData {
  label: string;
  value: number | string;
  icon: string;
  trend?: number;
  color?: string;
}

export function IFoodAnalyticsEnhancedPage() {
  const { branchId } = useAuthStore();
  const [periodStart, setPeriodStart] = useState(() => {
    const d = new Date();
    d.setDate(d.getDate() - 30);
    return d.toISOString().split("T")[0];
  });
  const [periodEnd, setPeriodEnd] = useState(new Date().toISOString().split("T")[0]);

  const kpisQuery = useQuery({
    queryKey: ["integrations", "ifood", "analytics", "kpis", branchId, periodStart, periodEnd],
    queryFn: () =>
      getIFoodOrderKpis(branchId, new Date(periodStart), new Date(periodEnd), 1),
  });

  const data = kpisQuery.data;

  // Parse JSON bruto dos buckets
  const parsedBuckets: Record<string, KpiData> = {};
  if (data?.buckets) {
    try {
      data.buckets.forEach((bucket, idx) => {
        const parsed = JSON.parse(bucket);
        // Ajustar conforme o schema real do iFood
        parsedBuckets[`bucket_${idx}`] = {
          label: parsed.label || `Métrica ${idx + 1}`,
          value: parsed.value || "—",
          icon: "📊",
          trend: parsed.trend,
          color: parsed.color,
        };
      });
    } catch {
      // Se não conseguir fazer parse, ignora
    }
  }

  if (kpisQuery.isLoading) {
    return (
      <main style={{ padding: 22, maxWidth: 1200, margin: "0 auto" }}>
        <PageHeader
          title="Analytics & Indicadores"
          subtitle="KPIs e performance da loja"
          breadcrumb={[{ label: "iFood", href: "/integracoes/ifood" }]}
        />
        <div style={{ textAlign: "center", padding: "40px 20px" }}>
          <p style={{ color: "var(--ink-faint)" }}>Carregando indicadores...</p>
        </div>
      </main>
    );
  }

  if (kpisQuery.isError) {
    return (
      <main style={{ padding: 22, maxWidth: 1200, margin: "0 auto" }}>
        <PageHeader
          title="Analytics & Indicadores"
          subtitle="KPIs e performance da loja"
          breadcrumb={[{ label: "iFood", href: "/integracoes/ifood" }]}
        />
        <QueryError error={kpisQuery.error} what="os indicadores analytics" />
      </main>
    );
  }

  return (
    <main style={{ padding: 22, maxWidth: 1200, margin: "0 auto" }}>
      <PageHeader
        title="Analytics & Indicadores"
        subtitle="KPIs e performance da loja no iFood"
        breadcrumb={[{ label: "iFood", href: "/integracoes/ifood" }]}
        actions={
          <Button
            variant="ghost"
            onClick={() => kpisQuery.refetch()}
            disabled={kpisQuery.isRefetching}
          >
            🔄 {kpisQuery.isRefetching ? "Atualizando..." : "Atualizar agora"}
          </Button>
        }
      />

      {/* Filtros de Período */}
      <div className="card" style={{ padding: 16, marginBottom: 20, display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(150px, 1fr))", gap: 12 }}>
        <TextField
          label="Data Inicial"
          type="date"
          value={periodStart}
          onChange={(e) => setPeriodStart(e.target.value)}
        />
        <TextField
          label="Data Final"
          type="date"
          value={periodEnd}
          onChange={(e) => setPeriodEnd(e.target.value)}
        />
        <div style={{ display: "flex", alignItems: "flex-end" }}>
          <Button variant="primary" onClick={() => kpisQuery.refetch()} style={{ width: "100%" }}>
            Filtrar
          </Button>
        </div>
      </div>

      {/* KPIs */}
      {Object.keys(parsedBuckets).length > 0 ? (
        <div style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fit, minmax(250px, 1fr))",
          gap: 12,
          marginBottom: 24,
        }}>
          {Object.values(parsedBuckets).map((kpi, idx) => (
            <DashboardCard
              key={idx}
              title={String(kpi.label)}
              value={kpi.value}
              icon={kpi.icon}
              status="info"
              trend={kpi.trend ? { direction: kpi.trend > 0 ? "up" : "down", percentage: Math.abs(kpi.trend) } : undefined}
            />
          ))}
        </div>
      ) : (
        <EmptyState
          title="Sem dados"
          description="Nenhum KPI disponível para o período selecionado. O iFood Analytics pode estar processando dados."
        />
      )}

      {/* JSON Bruto para Debug/Exportação */}
      {data?.buckets && data.buckets.length > 0 && (
        <div className="card" style={{ padding: 16, marginBottom: 20 }}>
          <h3 style={{ fontSize: "0.9rem", fontWeight: 700, margin: "0 0 12px" }}>
            📋 Dados Brutos (JSON)
          </h3>
          <div style={{ display: "grid", gap: 8 }}>
            {data.buckets.map((bucket, idx) => (
              <pre
                key={idx}
                style={{
                  padding: 12,
                  background: "var(--surface-2)",
                  borderRadius: 4,
                  fontSize: "0.8rem",
                  overflow: "auto",
                  margin: 0,
                  maxHeight: "200px",
                }}
              >
                {(() => {
                  try {
                    return JSON.stringify(JSON.parse(bucket), null, 2);
                  } catch {
                    return bucket;
                  }
                })()}
              </pre>
            ))}
          </div>
        </div>
      )}

      {/* Notas sobre Analytics */}
      <div className="card" style={{ padding: 16, background: "#fff3e0", borderLeft: "3px solid #f57c00" }}>
        <p style={{ fontSize: "0.9rem", margin: 0, lineHeight: 1.6 }}>
          <strong>⚠️ Nota sobre Analytics:</strong> Os dados do módulo Analytics do iFood retornam JSON bruto de "buckets" agregados.
          A estrutura exata dos dados depende de seus filtros de agregação no backend. Consulte a documentação completa para
          entender o shape de cada métrica.
        </p>
      </div>

      {/* Footer */}
      <div style={{ marginTop: 32 }}>
        <Link to="/integracoes/ifood" style={{ textDecoration: "none" }}>
          <Button variant="ghost">← Voltar ao iFood</Button>
        </Link>
      </div>
    </main>
  );
}
