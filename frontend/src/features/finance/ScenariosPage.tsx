import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getScenarios, setRevenueTarget } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { formatBRL } from "../../lib/types";
import type { ScenarioResponse } from "../../lib/types";
import { QueryError } from "../../components/QueryError";

const parsePct = (raw: string): number | undefined => {
  if (raw.trim() === "") return undefined;
  const value = Number(raw.replace(",", "."));
  return Number.isFinite(value) && value > 0 && value < 100 ? value / 100 : undefined;
};

const parseMoney = (raw: string): number => {
  const value = Number(raw.replace(",", "."));
  return Number.isFinite(value) && value >= 0 ? value : 0;
};

function currentMonthValue(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}`;
}

const scenarioTone: Record<string, string> = {
  Pessimista: "var(--danger)",
  Normal: "var(--amber)",
  Otimista: "var(--ok)",
};

export function ScenariosPage() {
  const queryClient = useQueryClient();
  const { branchId } = useAuthStore();
  const [monthValue, setMonthValue] = useState(currentMonthValue());
  const [profitInput, setProfitInput] = useState("");
  const [pessimisticInput, setPessimisticInput] = useState("");
  const [normalInput, setNormalInput] = useState("");
  const [optimisticInput, setOptimisticInput] = useState("");
  const [selected, setSelected] = useState<string>("Normal");
  const [feedback, setFeedback] = useState<string | null>(null);

  const [yearStr, monthStr] = monthValue.split("-");
  const year = Number(yearStr);
  const month = Number(monthStr);
  const desiredProfit = parseMoney(profitInput);

  const scenariosQuery = useQuery({
    queryKey: ["scenarios", branchId, year, month, desiredProfit,
      pessimisticInput, normalInput, optimisticInput],
    queryFn: () =>
      getScenarios(branchId, year, month, desiredProfit, {
        pessimistic: parsePct(pessimisticInput),
        normal: parsePct(normalInput),
        optimistic: parsePct(optimisticInput),
      }),
    enabled: Number.isFinite(year) && Number.isFinite(month),
    placeholderData: (previous) => previous,
  });

  const data = scenariosQuery.data;
  const selectedScenario: ScenarioResponse | undefined =
    data?.scenarios.find((s) => s.name === selected);

  const targetMutation = useMutation({
    mutationFn: (scenario: ScenarioResponse) =>
      setRevenueTarget({
        branchId,
        referenceYear: year,
        referenceMonth: month,
        targetAmount: scenario.targetRevenue,
      }),
    onSuccess: (_, scenario) => {
      setFeedback(`Meta do mês definida em ${formatBRL(scenario.targetRevenue)} (cenário ${scenario.name}).`);
      void queryClient.invalidateQueries({ queryKey: ["finance"] });
    },
    onError: (e) =>
      setFeedback(e instanceof ApiError ? e.message : "Falha ao definir a meta."),
  });

  return (
    <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }}>
      <div className="rise" style={{ display: "flex", alignItems: "center", gap: 14, marginBottom: 6, flexWrap: "wrap" }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>Cenários</h2>
        <span style={{ flex: 1 }} />
        <input type="month" value={monthValue} onChange={(e) => setMonthValue(e.target.value)} style={{ width: 190 }} />
      </div>
      <p className="rise" style={{ color: "var(--ink-faint)", fontSize: "0.9rem", marginTop: 0 }}>
        Quanto preciso faturar (e ter de estoque) para pagar todos os custos —
        faturamento necessário = (custos fixos + lucro desejado) ÷ margem de contribuição.
      </p>

      <div className="rise rise-1" style={{ display: "grid", gap: 8, gridTemplateColumns: "repeat(auto-fit, minmax(160px, 1fr))", marginBottom: 16 }}>
        <label style={{ display: "grid", gap: 4 }}>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.82rem" }}>Lucro desejado (R$)</span>
          <input inputMode="decimal" placeholder="0 = ponto de equilíbrio" value={profitInput} onChange={(e) => setProfitInput(e.target.value)} />
        </label>
        <label style={{ display: "grid", gap: 4 }}>
          <span style={{ color: "var(--danger)", fontSize: "0.82rem" }}>Margem pessimista (%)</span>
          <input inputMode="decimal" placeholder="auto" value={pessimisticInput} onChange={(e) => setPessimisticInput(e.target.value)} />
        </label>
        <label style={{ display: "grid", gap: 4 }}>
          <span style={{ color: "var(--amber)", fontSize: "0.82rem" }}>Margem normal (%)</span>
          <input inputMode="decimal" placeholder="auto (histórico)" value={normalInput} onChange={(e) => setNormalInput(e.target.value)} />
        </label>
        <label style={{ display: "grid", gap: 4 }}>
          <span style={{ color: "var(--ok)", fontSize: "0.82rem" }}>Margem otimista (%)</span>
          <input inputMode="decimal" placeholder="auto" value={optimisticInput} onChange={(e) => setOptimisticInput(e.target.value)} />
        </label>
      </div>

      {scenariosQuery.isError && <QueryError error={scenariosQuery.error} what="os cenários" />}
      {feedback && <p style={{ color: "var(--amber)" }}>{feedback}</p>}

      {data && (
        <>
          <p style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
            Base do mês: custos fixos {formatBRL(data.fixedCosts)}
            {data.historicalMarginRate !== null &&
              ` · margem real ${(data.historicalMarginRate * 100).toFixed(1)}%`}
            {data.historicalRevenue !== null && ` · receita até agora ${formatBRL(data.historicalRevenue)}`}
            {data.averageTicket !== null && ` · ticket médio ${formatBRL(data.averageTicket)}`}
            {data.historicalMarginRate === null && " · sem histórico no mês — usando margem padrão de 30%"}
          </p>

          <div className="rise rise-2" style={{ display: "grid", gap: 12, gridTemplateColumns: "repeat(auto-fit, minmax(240px, 1fr))", marginBottom: 16 }}>
            {data.scenarios.map((scenario) => {
              const tone = scenarioTone[scenario.name] ?? "var(--ink)";
              const isSelected = selected === scenario.name;
              return (
                <div
                  key={scenario.name}
                  role="button"
                  tabIndex={0}
                  onClick={() => setSelected(scenario.name)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter" || e.key === " ") setSelected(scenario.name);
                  }}
                  className="ticket"
                  style={{
                    padding: 18,
                    display: "grid",
                    gap: 8,
                    textAlign: "left",
                    cursor: "pointer",
                    border: isSelected ? `1px solid ${tone}` : "1px solid var(--line)",
                    background: "var(--bg-raise)",
                  }}
                >
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline" }}>
                    <span className="display" style={{ fontSize: "1.3rem", color: tone }}>
                      {scenario.name}
                    </span>
                    <span className="mono-num" style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
                      margem {(scenario.marginRate * 100).toFixed(1)}%
                    </span>
                  </div>
                  <div className="mono-num display" style={{ fontSize: "1.7rem" }}>
                    {formatBRL(scenario.targetRevenue)}
                  </div>
                  <div style={{ color: "var(--ink-dim)", fontSize: "0.85rem", display: "grid", gap: 2 }}>
                    <span>ponto de equilíbrio: <span className="mono-num">{formatBRL(scenario.breakEvenRevenue)}</span></span>
                    <span>por dia ({data.daysInMonth} dias): <span className="mono-num">{formatBRL(scenario.dailyTarget)}</span></span>
                    {scenario.estimatedSalesCount !== null && (
                      <span>≈ <span className="mono-num">{scenario.estimatedSalesCount}</span> vendas no mês</span>
                    )}
                  </div>
                  <button
                    className="btn-ghost"
                    style={{ minHeight: 40 }}
                    disabled={targetMutation.isPending}
                    onClick={(e) => {
                      e.stopPropagation();
                      targetMutation.mutate(scenario);
                    }}
                  >
                    Usar como meta do mês
                  </button>
                </div>
              );
            })}
          </div>

          {selectedScenario && (
            <div className="ticket rise rise-3">
              <div className="ticket-head">
                <span className="display" style={{ fontSize: "1.2rem" }}>
                  Estoque necessário — cenário {selectedScenario.name}
                </span>
                <span style={{ color: "var(--ink-faint)", fontSize: "0.82rem" }}>
                  baseado no mix real de vendas do mês
                </span>
              </div>
              {selectedScenario.stockPlan.map((item) => (
                <div className="ticket-row" key={item.productId}>
                  <div style={{ display: "grid", gap: 2 }}>
                    <span>{item.productName}</span>
                    <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                      {(item.revenueShare * 100).toFixed(1)}% da receita · precisa de{" "}
                      <span className="mono-num">{item.estimatedUnits}</span> un · em estoque{" "}
                      <span className="mono-num">{item.currentStock}</span>
                    </span>
                  </div>
                  <span
                    className="mono-num display"
                    style={{
                      fontSize: "1.2rem",
                      color: item.unitsToBuy > 0 ? "var(--danger)" : "var(--ok)",
                    }}
                  >
                    {item.unitsToBuy > 0 ? `comprar ${item.unitsToBuy}` : "ok"}
                  </span>
                </div>
              ))}
              {selectedScenario.stockPlan.length === 0 && (
                <div className="ticket-row" style={{ color: "var(--ink-faint)" }}>
                  Sem vendas com baixa de estoque neste mês — o plano de compra aparece
                  quando houver histórico de mix de vendas.
                </div>
              )}
            </div>
          )}
        </>
      )}
    </main>
  );
}
