import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Swal from "sweetalert2";
import { useDialog } from "../../ui/Dialog";
import { cancelReservation, confirmReservation, createReservation, getReservationsByBranch } from "./api";
import { getTablesByBranch } from "../tables/api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { ReservationStatus, TableStatus, reservationStatusLabel } from "../../lib/types";
import { QueryError } from "../../components/QueryError";
import { Modal } from "../../ui/Modal";
import { Button } from "../../ui/Button";
import { TextField, SelectField } from "../../ui/Field";
import { EmptyState } from "../../ui/EmptyState";
import { SkeletonList } from "../../ui/Skeleton";

function defaultRange() {
    const from = new Date();
    from.setHours(0, 0, 0, 0);
    const to = new Date(from);
    to.setDate(to.getDate() + 14);
    return { from: from.toISOString(), to: to.toISOString() };
}

export function ReservationsPage() {
    const queryClient = useQueryClient();
    const dialog = useDialog();
    const { branchId } = useAuthStore();
    const [creating, setCreating] = useState(false);
    const [confirmingId, setConfirmingId] = useState<number | null>(null);
    const [tableForConfirm, setTableForConfirm] = useState("");
    const range = useMemo(defaultRange, []);

    const [form, setForm] = useState({
        customerName: "",
        customerPhone: "",
        partySize: 2,
        reservedFor: "",
        notes: "",
    });

    const reservationsQuery = useQuery({
        queryKey: ["reservations", branchId, range.from, range.to],
        queryFn: () => getReservationsByBranch(branchId, range.from, range.to),
    });

    const tablesQuery = useQuery({
        queryKey: ["tables", branchId],
        queryFn: () => getTablesByBranch(branchId),
    });

    const refresh = () => void queryClient.invalidateQueries({ queryKey: ["reservations"] });

    const onApiError = (e: unknown) => {
        const message = e instanceof ApiError ? e.message : "Operação falhou.";
        Swal.fire({
            title: "Atenção",
            text: message,
            icon: "error",
            confirmButtonText: "Ok",
        });
    };

    const createMutation = useMutation({
        mutationFn: () =>
            createReservation({
                branchId,
                customerName: form.customerName.trim(),
                customerPhone: form.customerPhone.trim() === "" ? null : form.customerPhone.trim(),
                partySize: form.partySize,
                reservedFor: new Date(form.reservedFor).toISOString(),
                notes: form.notes.trim() === "" ? null : form.notes.trim(),
            }),
        onSuccess: () => {
            setCreating(false);
            setForm({ customerName: "", customerPhone: "", partySize: 2, reservedFor: "", notes: "" });
            refresh();
            Swal.fire({
                title: "Reserva criada!",
                text: "A nova reserva foi cadastrada com sucesso.",
                icon: "success",
                timer: 1500,
                showConfirmButton: false,
            });
        },
        onError: onApiError,
    });

    const confirmMutation = useMutation({
        mutationFn: (id: number) => confirmReservation(id, Number(tableForConfirm)),
        onSuccess: () => {
            setConfirmingId(null);
            setTableForConfirm("");
            refresh();
            void queryClient.invalidateQueries({ queryKey: ["tables"] });
            Swal.fire({
                title: "Reserva confirmada!",
                text: "A mesa foi vinculada e a reserva está confirmada.",
                icon: "success",
                timer: 1500,
                showConfirmButton: false,
            });
        },
        onError: onApiError,
    });

    const cancelMutation = useMutation({
        mutationFn: (id: number) => cancelReservation(id),
        onSuccess: () => {
            refresh();
            void queryClient.invalidateQueries({ queryKey: ["tables"] });
            Swal.fire({
                title: "Reserva cancelada",
                text: "A reserva foi cancelada com sucesso.",
                icon: "success",
                timer: 1500,
                showConfirmButton: false,
            });
        },
        onError: onApiError,
    });

    const sorted = [...(reservationsQuery.data ?? [])].sort(
        (a, b) => new Date(a.reservedFor).getTime() - new Date(b.reservedFor).getTime(),
    );

    const freeTables = (tablesQuery.data ?? []).filter((t) => t.tableStatusId === TableStatus.Livre);

    return (
        <main style={{ padding: 22, maxWidth: 1000, margin: "0 auto", position: "relative" }}>
            <div className="rise" style={{ display: "flex", alignItems: "baseline", gap: 14, marginBottom: 6, flexWrap: "wrap" }}>
                <h2 className="display" style={{ fontSize: "1.7rem" }}>Reservas de mesa</h2>
                <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>próximos 14 dias</span>
                <span style={{ flex: 1 }} />
                <button className="btn-primary" data-testid="btn-new-reservation" type="button" onClick={() => setCreating(true)}>
                    + Nova reserva
                </button>
            </div>

            {reservationsQuery.isError && <QueryError error={reservationsQuery.error} what="as reservas" />}

            {reservationsQuery.isLoading && <SkeletonList rows={4} rowHeight={80} />}

            {!reservationsQuery.isLoading && sorted.length === 0 && (
                <EmptyState
                    icon="📅"
                    title="Nenhuma reserva nos próximos 14 dias"
                    description="Crie uma reserva para reservar uma mesa com antecedência."
                    action={
                        <button className="btn-primary" data-testid="btn-empty-new-reservation" type="button" onClick={() => setCreating(true)}>
                            + Nova reserva
                        </button>
                    }
                />
            )}

            {!reservationsQuery.isLoading && sorted.length > 0 && (
                <div className="rise rise-1" style={{ display: "grid", gap: 10, marginTop: 12 }} data-testid="reservations-list">
                    {sorted.map((r) => (
                        <div key={r.id} className="ticket" data-testid={`reservation-item-${r.id}`}>
                            <div className="ticket-head">
                                <span>{r.customerName}</span>
                                <span className="chip" style={{ "--dot": "var(--busy)" } as React.CSSProperties}>
                                    {reservationStatusLabel[r.reservationStatusId]}
                                </span>
                            </div>
                            <div className="ticket-row" style={{ alignItems: "center" }}>
                                <div style={{ display: "grid", gap: 2 }}>
                                    <span className="mono-num">
                                        {new Date(r.reservedFor).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" })}
                                    </span>
                                    <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                                        {r.partySize} pessoas {r.customerPhone ? `· ${r.customerPhone}` : ""}
                                    </span>
                                </div>
                                {r.reservationStatusId === ReservationStatus.Pending && (
                                    <div style={{ display: "flex", gap: 8 }}>
                                        <button
                                            className="btn-ghost"
                                            type="button"
                                            data-testid={`btn-confirm-${r.id}`}
                                            style={{ minHeight: 44, padding: "0 10px", fontSize: "0.85rem" }}
                                            onClick={() => setConfirmingId(r.id)}
                                        >
                                            Confirmar
                                        </button>
                                        <button
                                            className="btn-danger"
                                            type="button"
                                            data-testid={`btn-cancel-${r.id}`}
                                            style={{ minHeight: 44, padding: "0 10px", fontSize: "0.85rem" }}
                                            onClick={async () => {
                                                if (await dialog.confirm({ title: "Cancelar reserva", message: `Cancelar a reserva de ${r.customerName}?`, confirmLabel: "Cancelar reserva", danger: true }))
                                                    cancelMutation.mutate(r.id);
                                            }}
                                        >
                                            Cancelar
                                        </button>
                                    </div>
                                )}
                                {r.reservationStatusId === ReservationStatus.Confirmed && (
                                    <button
                                        className="btn-danger"
                                        type="button"
                                        data-testid={`btn-cancel-${r.id}`}
                                        style={{ minHeight: 44, padding: "0 10px", fontSize: "0.85rem" }}
                                        onClick={async () => {
                                            if (await dialog.confirm({ title: "Cancelar reserva", message: `Cancelar a reserva de ${r.customerName}?`, confirmLabel: "Cancelar reserva", danger: true }))
                                                cancelMutation.mutate(r.id);
                                        }}
                                    >
                                        Cancelar
                                    </button>
                                )}
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {creating && (
                <Modal title="Nova reserva" onClose={() => setCreating(false)} variant="center">
                    <TextField
                        data-testid="input-customer-name"
                        label="Nome do cliente"
                        type="text"
                        value={form.customerName}
                        onChange={(e) => setForm((f) => ({ ...f, customerName: e.target.value }))}
                        autoFocus
                    />

                    <div className="ui-row ui-row-wrap">
                        <div style={{ flex: 1, minWidth: 140 }}>
                            <TextField
                                data-testid="input-customer-phone"
                                label="Telefone"
                                type="text"
                                value={form.customerPhone}
                                onChange={(e) => setForm((f) => ({ ...f, customerPhone: e.target.value }))}
                            />
                        </div>
                        <div style={{ flex: 1, minWidth: 100 }}>
                            <TextField
                                data-testid="input-party-size"
                                label="Pessoas"
                                type="number"
                                min={1}
                                value={form.partySize}
                                onChange={(e) => setForm((f) => ({ ...f, partySize: Number(e.target.value) }))}
                            />
                        </div>
                    </div>

                    <TextField
                        data-testid="input-reserved-for"
                        label="Data e hora"
                        type="datetime-local"
                        value={form.reservedFor}
                        onChange={(e) => setForm((f) => ({ ...f, reservedFor: e.target.value }))}
                    />

                    <TextField
                        data-testid="input-notes"
                        label="Observações"
                        type="text"
                        value={form.notes}
                        onChange={(e) => setForm((f) => ({ ...f, notes: e.target.value }))}
                    />

                    <Button
                        data-testid="btn-submit-reservation"
                        variant="primary"
                        block
                        loading={createMutation.isPending}
                        disabled={form.customerName.trim() === "" || form.reservedFor === ""}
                        onClick={() => createMutation.mutate()}
                    >
                        Criar reserva
                    </Button>
                </Modal>
            )}

            {confirmingId !== null && (
                <Modal title="Confirmar reserva — escolher mesa" onClose={() => setConfirmingId(null)} variant="center">
                    <SelectField data-testid="select-free-table" label="Mesa livre" value={tableForConfirm} onChange={(e) => setTableForConfirm(e.target.value)} autoFocus>
                        <option value="">Selecione…</option>
                        {freeTables.map((t) => (
                            <option key={t.id} value={t.id}>Mesa {t.number}</option>
                        ))}
                    </SelectField>
                    {freeTables.length === 0 && (
                        <p style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>Nenhuma mesa livre no momento.</p>
                    )}
                    <Button
                        data-testid="btn-submit-confirm-reservation"
                        variant="primary"
                        block
                        loading={confirmMutation.isPending}
                        disabled={tableForConfirm === ""}
                        onClick={() => confirmMutation.mutate(confirmingId)}
                    >
                        Confirmar e reservar mesa
                    </Button>
                </Modal>
            )}
        </main>
    );
}