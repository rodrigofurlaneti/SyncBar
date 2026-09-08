import { useState, type CSSProperties } from "react";
import { useQuery } from "@tanstack/react-query";
import { getShiftHistory } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ShiftClosingStatus, formatBRL } from "../../lib/types";
import type { ShiftClosingResponse } from "../../lib/types";
import { QueryError } from "../../components/QueryError";

function currentMonthValue(): string {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}`;
}

function differenceBadge(shift: ShiftClosingResponse) {
    if (shift.shiftClosingStatusId === ShiftClosingStatus.Aberto)
        return { label: "Em aberto", color: "var(--reserved)" };
    const diff = shift.totalDifferenceAmount;
    if (diff === 0) return { label: "Bateu", color: "var(--ok)" };
    if (diff > 0) return { label: `Sobra ${formatBRL(diff)}`, color: "var(--busy)" };
    return { label: `Falta ${formatBRL(Math.abs(diff))}`, color: "var(--danger)" };
}

const fmtTime = (iso: string) =>
    new Date(iso).toLocaleString("pt-BR", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" });

export function ShiftHistoryPage() {
    const { branchId } = useAuthStore();
    const [monthValue, setMonthValue] = useState(currentMonthValue());

    const [yearStr, monthStr] = monthValue.split("-");
    const year = Number(yearStr);
    const month = Number(monthStr);

    const historyQuery = useQuery({
        queryKey: ["shift", "history", branchId, year, month],
        queryFn: () => getShiftHistory(branchId, year, month),
        enabled: Number.isFinite(year) && Number.isFinite(month),
    });

    const shifts = historyQuery.data ?? [];
    const closed = shifts.filter((s) => s.shiftClosingStatusId !== ShiftClosingStatus.Aberto);
    const totalDiff = closed.reduce((sum, s) => sum + s.totalDifferenceAmount, 0);
    const divergent = closed.filter((s) => s.totalDifferenceAmount !== 0).length;

    return (
        <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }}>
            <div className="rise" style={{ display: "flex", alignItems: "center", gap: 14, marginBottom: 6, flexWrap: "wrap" }}>
                <h2 className="display" style={{ fontSize: "1.7rem" }}>Fechamentos de turno</h2>
                <span style={{ flex: 1 }} />
                <input
                    type="month"
                    value={monthValue}
                    onChange={(e) => setMonthValue(e.target.value)}
                    style={{ width: 190 }}
                    data-testid="month-filter-input"
                />
            </div>

            {closed.length > 0 && (
                <p className="rise" style={{ color: "var(--ink-dim)", fontSize: "0.9rem", marginTop: 0 }} data-testid="summary-text">
                    {closed.length} turnos fechados no mês · {divergent === 0 ? "todos bateram" : `${divergent} com divergência`} ·
                    saldo das diferenças:{" "}
                    <span className="mono-num" style={{ color: totalDiff === 0 ? "var(--ok)" : totalDiff > 0 ? "var(--busy)" : "var(--danger)", fontWeight: 700 }}>
                        {totalDiff > 0 ? "+" : ""}{formatBRL(totalDiff)}
                    </span>
                </p>
            )}

            {historyQuery.isError && <QueryError error={historyQuery.error} what="os fechamentos de turno" />}

            <div className="ticket rise rise-1" data-testid="history-list">
                {shifts.map((shift) => {
                    const badge = differenceBadge(shift);
                    return (
                        <div className="ticket-row" key={shift.id} style={{ flexWrap: "wrap", gap: 10 }} data-testid={`shift-row-${shift.id}`}>
                            <div style={{ display: "grid", gap: 3, minWidth: 220 }}>
                                <span>Turno #{shift.id}</span>
                                <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                                    {fmtTime(shift.periodStart)}
                                    {shift.periodEnd ? ` → ${fmtTime(shift.periodEnd)}` : " → em aberto"}
                                </span>
                                <span className="mono-num" style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                                    {shift.cashSessionsCount} caixas · fundo {formatBRL(shift.totalOpeningAmount)}
                                    {shift.shiftClosingStatusId !== ShiftClosingStatus.Aberto && (
                                        <>
                                            {" "}· esperado {formatBRL(shift.totalExpectedAmount)} · realizado {formatBRL(shift.totalRealizedAmount)}
                                        </>
                                    )}
                                </span>
                                {shift.notes && (
                                    <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>{shift.notes}</span>
                                )}
                            </div>
                            <div style={{ display: "flex", gap: 10, alignItems: "center", marginLeft: "auto" }}>
                                <span
                                    className="chip"
                                    data-testid={`badge-${shift.id}`}
                                    style={{ "--dot": badge.color, color: badge.color, borderColor: badge.color } as CSSProperties}
                                >
                                    {badge.label}
                                </span>
                            </div>
                        </div>
                    );
                })}
                {shifts.length === 0 && !historyQuery.isLoading && (
                    <div className="ticket-row" style={{ color: "var(--ink-faint)" }} data-testid="empty-history-msg">
                        Nenhum turno fechado neste mês.
                    </div>
                )}
            </div>
        </main>
    );
}
