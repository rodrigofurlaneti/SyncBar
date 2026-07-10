import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createOperatingCost,
  deactivateOperatingCost,
  getBillingSummary,
  setRevenueTarget,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { CostType, costTypeLabel, formatBRL } from "../../lib/types";
import { QueryError } from "../../components/QueryError";

const parseNum = (raw: string): number | null => {
  if (raw.trim() === "") return null;
  const value = Number(raw.replace(",", "."));
  return Number.isFinite(value) && value > 0 ? value : null;
};

function currentMonthValue(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}`;
}

function Card({ label, value, tone }: { label: string; value: string; tone?: string }) {
  return (
    <div
      className="ticket"
      style={{ padding: "14px 18px", display: "grid", gap: 4, minWidth: 0 }}
    >
      <span
        style={{
          fontFamily: "var(--font-cond)",
          textTransform: "uppercase",
          letterSpacing: "0.08em",
          fontSize: "0.78rem",
          color: "var(--ink-faint)",
        }}
      >
        {label}
      </span>
      <span className="mono-num display" style={{ fontSize: "1.6rem", color: tone ?? "var(--ink)" }}>
        {value}
      </span>
    </div>
  );
}

export function FinancePage() {
  const queryClient = useQueryClient();
  const { branchId } = useAuthStore();
  const [monthValue, setMonthValue] = useState(currentMonthValue());
  const [description, setDescription] = useState("");
  const [costTypeId, setCostTypeId] = useState<number>(CostType.Fixo);
  const [amount, setAmount] = useState("");
  const [targetInput, setTargetInput] = useState("");
  const [error, setError] = useState<string | null>(null);

  const [yearStr, monthStr] = monthValue.split("-");
  const year = Number(yearStr);
  const month = Number(monthStr);

  const summaryQuery = useQuery({
    queryKey: ["finance", branchId, year, month],
    queryFn: () => getBillingSummary(branchId, year, month),
    enabled: Number.isFinite(year) && Number.isFinite(month),
  });

  const summary = summaryQuery.data;
  const refresh = () => void queryClient.invalidateQueries({ queryKey: ["finance"] });
  const onApiError = (e: unknown) =>
    setError(e instanceof ApiError ? e.message : "Operação falhou.");

  const costMutation = useMutation({
    mutationFn: () =>
      createOperatingCost({
        branchId,
        costTypeId,
        description: description.trim(),
        amount: parseNum(amount) ?? 0,
        referenceYear: year,
        referenceMonth: month,
      }),
    onSuccess: () => {
      setError(null);
      setDescription("");
      setAmount("");
      refresh();
    },
    onError: onApiError,
  });

  const removeCostMutation = useMutation({
    mutationFn: (id: number) => deactivateOperatingCost(id),
    onSuccess: refresh,
    onError: onApiError,
  });

  const targetMutation = useMutation({
    mutationFn: () =>
      setRevenueTarget({
        branchId,
        referenceYear: year,
        referenceMonth: month,
        targetAmount: parseNum(targetInput) ?? 0,
      }),
    onSuccess: () => {
      setError(null);
      setTargetInput("");
      refresh();
    },
    onError: onApiError,
  });

  const maxDaily = useMemo(
    () => Math.max(1, ...(summary?.dailyRevenue ?? []).map((d) => d.amount)),
    [summary?.dailyRevenue],
  );

  const attainment = summary?.targetAttainmentRate ?? null;

  return (
    <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }}>
      <div className="rise" style={{ display: "flex", alignItems: "center", gap: 14, marginBottom: 16, flexWrap: "wrap" }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>Faturamento</h2>
        <span style={{ flex: 1 }} />
        <input
          type="month"
          value={monthValue}
          onChange={(e) => setMonthValue(e.target.value)}
          style={{ width: 190 }}
        />
      </div>

      {summaryQuery.isError && <QueryError error={summaryQuery.error} what="o resumo do mês" />}
      {error && <p className="error-text">{error}</p>}

      {summary && (
        <>
          <div
            className="rise rise-1"
            style={{ display: "grid", gap: 12, gridTemplateColumns: "repeat(auto-fit, minmax(170px, 1fr))", marginBottom: 16 }}
          >
            <Card label={`Receita (${summary.salesCount} vendas)`} value={formatBRL(summary.revenue)} tone="var(--amber)" />
            <Card label="CMV (produtos vendidos)" value={formatBRL(summary.costOfGoodsSold)} />
            <Card label="Custos fixos" value={formatBRL(summary.fixedCosts)} />
            <Card label="Custos variáveis" value={formatBRL(summary.variableCosts)} />
            <Card
              label="Resultado"
              value={formatBRL(summary.netResult)}
              tone={summary.netResult >= 0 ? "var(--ok)" : "var(--danger)"}
            />
          </div>

          <div className="ticket rise rise-2" style={{ padding: 18, display: "grid", gap: 10, marginBottom: 16 }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline", flexWrap: "wrap", gap: 8 }}>
              <span className="display" style={{ fontSize: "1.2rem" }}>Meta do mês</span>
              {summary.targetAmount !== null ? (
                <span className="mono-num" style={{ color: "var(--ink-dim)" }}>
                  {formatBRL(summary.revenue)} de {formatBRL(summary.targetAmount)}
                  {attainment !== null && (
                    <strong style={{ color: attainment >= 1 ? "var(--ok)" : "var(--amber)", marginLeft: 8 }}>
                      {(attainment * 100).toFixed(1)}%
                    </strong>
                  )}
                </span>
              ) : (
                <span style={{ color: "var(--ink-faint)" }}>nenhuma meta definida</span>
              )}
            </div>

            {summary.targetAmount !== null && (
              <div style={{ background: "var(--bg-press)", borderRadius: 999, height: 14, overflow: "hidden" }}>
                <div
                  style={{
                    width: `${Math.min(100, (attainment ?? 0) * 100)}%`,
                    height: "100%",
                    background: (attainment ?? 0) >= 1 ? "var(--ok)" : "var(--amber)",
                    borderRadius: 999,
                    transition: "width 300ms ease",
                  }}
                />
              </div>
            )}

            <div style={{ display: "flex", gap: 8, maxWidth: 420 }}>
              <input
                placeholder={summary.targetAmount !== null ? "Nova meta (R$)" : "Definir meta (R$)"}
                inputMode="decimal"
                value={targetInput}
                onChange={(e) => setTargetInput(e.target.value)}
              />
              <button
                className="btn-ghost"
                disabled={parseNum(targetInput) === null || targetMutation.isPending}
                onClick={() => targetMutation.mutate()}
              >
                Salvar meta
              </button>
            </div>
          </div>

          {summary.dailyRevenue.length > 0 && (
            <div className="ticket rise rise-2" style={{ padding: 18, marginBottom: 16 }}>
              <span className="display" style={{ fontSize: "1.2rem" }}>Receita por dia</span>
              <div style={{ display: "flex", alignItems: "flex-end", gap: 4, height: 120, marginTop: 12 }}>
                {summary.dailyRevenue.map((d) => (
                  <div key={d.day} style={{ flex: 1, display: "grid", gap: 4, justifyItems: "center" }}>
                    <div
                      title={`Dia ${d.day}: ${formatBRL(d.amount)}`}
                      style={{
                        width: "100%",
                        maxWidth: 34,
                        height: `${Math.max(4, (d.amount / maxDaily) * 100)}px`,
                        background: "var(--amber)",
                        borderRadius: "4px 4px 0 0",
                        opacity: 0.9,
                      }}
                    />
                    <span className="mono-num" style={{ fontSize: "0.7rem", color: "var(--ink-faint)" }}>
                      {d.day}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )}

          <div className="ticket rise rise-3">
            <div className="ticket-head">
              <span className="display" style={{ fontSize: "1.2rem" }}>Custos do mês</span>
              <span className="mono-num" style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
                total {formatBRL(summary.fixedCosts + summary.variableCosts)}
              </span>
            </div>

            {summary.costs.map((cost) => (
              <div className="ticket-row" key={cost.id}>
                <div style={{ display: "grid", gap: 2 }}>
                  <span>{cost.description}</span>
                  <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                    {costTypeLabel[cost.costTypeId]}
                  </span>
                </div>
                <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
                  <span className="mono-num">{formatBRL(cost.amount)}</span>
                  <button
                    className="btn-danger"
                    style={{ minHeight: 36, padding: "0 10px", fontSize: "0.85rem" }}
                    disabled={removeCostMutation.isPending}
                    onClick={() => {
                      if (window.confirm(`Remover o custo "${cost.description}"?`))
                        removeCostMutation.mutate(cost.id);
                    }}
                  >
                    ✕
                  </button>
                </div>
              </div>
            ))}
            {summary.costs.length === 0 && (
              <div className="ticket-row" style={{ color: "var(--ink-faint)" }}>
                Nenhum custo lançado neste mês.
              </div>
            )}

            <div className="ticket-row" style={{ gap: 8, flexWrap: "wrap" }}>
              <input
                placeholder="Descrição (ex.: Aluguel)"
                style={{ flex: 2, minWidth: 160 }}
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
              <select
                style={{ flex: 1, minWidth: 110, width: "auto" }}
                value={costTypeId}
                onChange={(e) => setCostTypeId(Number(e.target.value))}
              >
                <option value={CostType.Fixo}>Fixo</option>
                <option value={CostType.Variavel}>Variável</option>
              </select>
              <input
                placeholder="Valor (R$)"
                inputMode="decimal"
                style={{ flex: 1, minWidth: 110 }}
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
              />
              <button
                className="btn-primary"
                disabled={description.trim() === "" || parseNum(amount) === null || costMutation.isPending}
                onClick={() => costMutation.mutate()}
              >
                + Lançar custo
              </button>
            </div>
          </div>
        </>
      )}
    </main>
  );
}
