import { useState, type CSSProperties } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getCashHistory, reviewCashSession } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { CashSessionStatus, formatBRL } from "../../lib/types";
import type { CashSessionHistoryResponse } from "../../lib/types";
import { QueryError } from "../../components/QueryError";

function currentMonthValue(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}`;
}

function differenceBadge(session: CashSessionHistoryResponse) {
  if (session.cashSessionStatusId === CashSessionStatus.Aberto)
    return { label: "Em aberto", color: "var(--reserved)" };
  const diff = session.differenceAmount ?? 0;
  if (diff === 0) return { label: "Bateu", color: "var(--ok)" };
  if (diff > 0) return { label: `Sobra ${formatBRL(diff)}`, color: "var(--busy)" };
  return { label: `Falta ${formatBRL(Math.abs(diff))}`, color: "var(--danger)" };
}

const fmtTime = (iso: string) =>
  new Date(iso).toLocaleString("pt-BR", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" });

export function CashHistoryPage() {
  const queryClient = useQueryClient();
  const { branchId } = useAuthStore();
  const [monthValue, setMonthValue] = useState(currentMonthValue());
  const [error, setError] = useState<string | null>(null);

  const [yearStr, monthStr] = monthValue.split("-");
  const year = Number(yearStr);
  const month = Number(monthStr);

  const historyQuery = useQuery({
    queryKey: ["cash", "history", branchId, year, month],
    queryFn: () => getCashHistory(branchId, year, month),
    enabled: Number.isFinite(year) && Number.isFinite(month),
  });

  const reviewMutation = useMutation({
    mutationFn: (id: number) => reviewCashSession(id),
    onSuccess: () => {
      setError(null);
      void queryClient.invalidateQueries({ queryKey: ["cash", "history"] });
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "Falha ao conferir."),
  });

  const sessions = historyQuery.data ?? [];
  const closed = sessions.filter((s) => s.cashSessionStatusId !== CashSessionStatus.Aberto);
  const totalDiff = closed.reduce((sum, s) => sum + (s.differenceAmount ?? 0), 0);
  const divergent = closed.filter((s) => (s.differenceAmount ?? 0) !== 0).length;

  return (
    <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }}>
      <div className="rise" style={{ display: "flex", alignItems: "center", gap: 14, marginBottom: 6, flexWrap: "wrap" }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>Fechamentos de caixa</h2>
        <span style={{ flex: 1 }} />
        <input type="month" value={monthValue} onChange={(e) => setMonthValue(e.target.value)} style={{ width: 190 }} />
      </div>

      {closed.length > 0 && (
        <p className="rise" style={{ color: "var(--ink-dim)", fontSize: "0.9rem", marginTop: 0 }}>
          {closed.length} fechamentos no mês · {divergent === 0 ? "todos bateram" : `${divergent} com divergência`} ·
          saldo das diferenças:{" "}
          <span className="mono-num" style={{ color: totalDiff === 0 ? "var(--ok)" : totalDiff > 0 ? "var(--busy)" : "var(--danger)", fontWeight: 700 }}>
            {totalDiff > 0 ? "+" : ""}{formatBRL(totalDiff)}
          </span>
        </p>
      )}

      {historyQuery.isError && <QueryError error={historyQuery.error} what="os fechamentos" />}
      {error && <p className="error-text">{error}</p>}

      <div className="ticket rise rise-1">
        {sessions.map((session) => {
          const badge = differenceBadge(session);
          return (
            <div className="ticket-row" key={session.id} style={{ flexWrap: "wrap", gap: 10 }}>
              <div style={{ display: "grid", gap: 3, minWidth: 220 }}>
                <span>
                  {session.cashRegisterName} · sessão #{session.id}
                  {session.cashSessionStatusId === CashSessionStatus.Conferido && (
                    <span style={{ color: "var(--ok)", fontSize: "0.78rem", marginLeft: 8 }}>✓ conferido</span>
                  )}
                </span>
                <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                  {fmtTime(session.openedAt)}
                  {session.closedAt ? ` → ${fmtTime(session.closedAt)}` : " → em aberto"}
                  {session.openedByName ? ` · abriu ${session.openedByName}` : ""}
                  {session.closedByName ? ` · fechou ${session.closedByName}` : ""}
                </span>
                <span className="mono-num" style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                  {session.salesCount} vendas ({formatBRL(session.salesTotal)}) · fundo {formatBRL(session.openingAmount)}
                  {session.expectedAmount !== null && ` · esperado ${formatBRL(session.expectedAmount)}`}
                  {session.closingAmount !== null && ` · contado ${formatBRL(session.closingAmount)}`}
                </span>
              </div>
              <div style={{ display: "flex", gap: 10, alignItems: "center", marginLeft: "auto" }}>
                <span
                  className="chip"
                  style={{ "--dot": badge.color, color: badge.color, borderColor: badge.color } as CSSProperties}
                >
                  {badge.label}
                </span>
                {session.cashSessionStatusId === CashSessionStatus.Fechado && (
                  <button
                    className="btn-ghost"
                    style={{ minHeight: 38, padding: "0 12px", fontSize: "0.85rem" }}
                    disabled={reviewMutation.isPending}
                    onClick={() => reviewMutation.mutate(session.id)}
                  >
                    Marcar conferido
                  </button>
                )}
              </div>
            </div>
          );
        })}
        {sessions.length === 0 && !historyQuery.isLoading && (
          <div className="ticket-row" style={{ color: "var(--ink-faint)" }}>
            Nenhuma sessão de caixa neste mês.
          </div>
        )}
      </div>
    </main>
  );
}
