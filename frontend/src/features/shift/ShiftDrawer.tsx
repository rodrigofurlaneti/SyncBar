import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Swal from "sweetalert2";
import { closeShift, getOpenShift, openShift } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { formatBRL } from "../../lib/types";
import type { ShiftClosingResponse } from "../../lib/types";
import { Overlay } from "../orders/Overlay";

interface Props {
    onClose: () => void;
}

const fmtDateTime = (iso: string) =>
    new Date(iso).toLocaleString("pt-BR", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" });

interface DifferenceState {
    label: string;
    color: string;
}

const getDifferenceState = (differenceAmount: number): DifferenceState => {
    if (differenceAmount === 0) return { label: "Bateu — sem diferença", color: "var(--ok)" };
    if (differenceAmount > 0) return { label: "Sobra", color: "var(--amber)" };
    return { label: "Falta", color: "var(--danger)" };
};

export function ShiftDrawer({ onClose }: Props) {
    const queryClient = useQueryClient();
    const { employeeId, branchId } = useAuthStore();
    const [notes, setNotes] = useState("");
    const [closeResult, setCloseResult] = useState<ShiftClosingResponse | null>(null);
    const [error, setError] = useState<string | null>(null);

    const shiftQuery = useQuery({
        queryKey: ["shift", "open", branchId],
        queryFn: () => getOpenShift(branchId),
        retry: false,
    });

    const noShift =
        shiftQuery.isError &&
        shiftQuery.error instanceof ApiError &&
        shiftQuery.error.status === 404 &&
        shiftQuery.error.code === "ShiftClosing.NotFound";

    const invalidateShift = () => void queryClient.invalidateQueries({ queryKey: ["shift"] });

    const onApiError = (e: unknown, fallback: string) =>
        setError(e instanceof ApiError ? e.message : fallback);

    const openMutation = useMutation({
        mutationFn: () => openShift(branchId, employeeId ?? 1),
        onSuccess: () => {
            setError(null);
            invalidateShift();
        },
        onError: (e) => onApiError(e, "Falha ao abrir o turno."),
    });

    const closeMutation = useMutation({
        mutationFn: (shiftClosingId: number) =>
            closeShift(shiftClosingId, employeeId ?? 1, notes.trim() === "" ? null : notes.trim()),
        onSuccess: (result) => {
            setError(null);
            setCloseResult(result);
            invalidateShift();
        },
        onError: (e) => onApiError(e, "Falha ao fechar o turno."),
    });

    const shift = shiftQuery.data;

    return (
        <Overlay title="Turno Comercial" onClose={onClose} wide data-testid="shift-drawer-overlay">
            {shiftQuery.isLoading && <p style={{ color: "var(--ink-dim)" }} data-testid="loading-text">Carregando…</p>}

            {shiftQuery.isError && !noShift && (
                <p className="error-text" role="alert" data-testid="shift-query-error">
                    {shiftQuery.error instanceof ApiError ? shiftQuery.error.message : "Não foi possível consultar o turno."}
                </p>
            )}

            {closeResult && (
                <div className="ticket" style={{ padding: 18, display: "grid", gap: 8 }} data-testid="close-result-view">
                    <div className="display" style={{ fontSize: "1.3rem" }}>Turno fechado</div>
                    <div style={{ display: "flex", justifyContent: "space-between", color: "var(--ink-dim)" }}>
                        <span>Caixas consolidados</span>
                        <span className="mono-num">{closeResult.cashSessionsCount}</span>
                    </div>
                    <div style={{ display: "flex", justifyContent: "space-between", color: "var(--ink-dim)" }}>
                        <span>Fundo de troco total</span>
                        <span className="mono-num">{formatBRL(closeResult.totalOpeningAmount)}</span>
                    </div>
                    <div style={{ display: "flex", justifyContent: "space-between", color: "var(--ink-dim)" }}>
                        <span>Esperado</span>
                        <span className="mono-num">{formatBRL(closeResult.totalExpectedAmount)}</span>
                    </div>
                    <div style={{ display: "flex", justifyContent: "space-between", color: "var(--ink-dim)" }}>
                        <span>Realizado</span>
                        <span className="mono-num">{formatBRL(closeResult.totalRealizedAmount)}</span>
                    </div>
                    {(() => {
                        const state = getDifferenceState(closeResult.totalDifferenceAmount);
                        return (
                            <div style={{ display: "flex", justifyContent: "space-between", fontWeight: 700, color: state.color }}>
                                <span>{state.label}</span>
                                <span className="mono-num">{formatBRL(Math.abs(closeResult.totalDifferenceAmount))}</span>
                            </div>
                        );
                    })()}
                </div>
            )}

            {!closeResult && noShift && (
                <div style={{ display: "grid", gap: 12 }} data-testid="open-shift-view">
                    <p style={{ color: "var(--ink-dim)" }}>Nenhum turno aberto nesta filial.</p>
                    {error && <p className="error-text" data-testid="error-message">{error}</p>}
                    <button
                        type="button"
                        className="btn-primary"
                        disabled={openMutation.isPending}
                        onClick={() => openMutation.mutate()}
                        data-testid="open-shift-btn"
                    >
                        {openMutation.isPending ? "Abrindo…" : "Abrir turno"}
                    </button>
                </div>
            )}

            {!closeResult && shift && (
                <div style={{ display: "grid", gap: 12 }} data-testid="close-shift-view">
                    <div className="ticket">
                        <div className="ticket-head">
                            <span className="display" style={{ fontSize: "1.2rem" }}>Turno aberto</span>
                            <span className="mono-num" style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
                                #{shift.id}
                            </span>
                        </div>
                        <div className="ticket-row" style={{ color: "var(--ink-dim)" }}>
                            <span>Aberto em</span>
                            <span className="mono-num">{fmtDateTime(shift.periodStart)}</span>
                        </div>
                    </div>

                    <textarea
                        placeholder="Observações do fechamento (opcional)"
                        value={notes}
                        onChange={(e) => setNotes(e.target.value)}
                        rows={3}
                        data-testid="close-shift-notes-input"
                    />

                    {error && <p className="error-text" data-testid="error-message">{error}</p>}

                    <button
                        type="button"
                        className="btn-danger"
                        disabled={closeMutation.isPending}
                        data-testid="close-shift-btn"
                        onClick={async () => {
                            const { isConfirmed } = await Swal.fire({
                                title: "Fechar turno",
                                text: "Fechar o turno comercial? Todos os caixas da filial no período precisam estar fechados.",
                                icon: "warning",
                                showCancelButton: true,
                                confirmButtonColor: "#d33",
                                confirmButtonText: "Fechar turno",
                                cancelButtonText: "Cancelar",
                            });

                            if (isConfirmed) {
                                closeMutation.mutate(shift.id);
                            }
                        }}
                    >
                        {closeMutation.isPending ? "Fechando…" : "Fechar turno"}
                    </button>
                </div>
            )}
        </Overlay>
    );
}
