import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useDialog } from "../../ui/Dialog";
import { cancelReservation, confirmReservation, createReservation, getReservationsByBranch } from "./api";
import { getTablesByBranch } from "../tables/api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { ReservationStatus, TableStatus, reservationStatusLabel } from "../../lib/types";
import { QueryError } from "../../components/QueryError";
import { Overlay } from "../orders/Overlay";

// Janela padrão: hoje até 14 dias à frente — cobre a agenda de curto prazo sem paginação.
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
  const [error, setError] = useState<string | null>(null);
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
  const onApiError = (e: unknown) => setError(e instanceof ApiError ? e.message : "Operação falhou.");

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
      setError(null);
      setCreating(false);
      setForm({ customerName: "", customerPhone: "", partySize: 2, reservedFor: "", notes: "" });
      refresh();
    },
    onError: onApiError,
  });

  const confirmMutation = useMutation({
    mutationFn: (id: number) => confirmReservation(id, Number(tableForConfirm)),
    onSuccess: () => {
      setError(null);
      setConfirmingId(null);
      setTableForConfirm("");
      refresh();
      void queryClient.invalidateQueries({ queryKey: ["tables"] });
    },
    onError: onApiError,
  });

  const cancelMutation = useMutation({
    mutationFn: (id: number) => cancelReservation(id),
    onSuccess: () => {
      refresh();
      void queryClient.invalidateQueries({ queryKey: ["tables"] });
    },
    onError: onApiError,
  });

  const sorted = [...(reservationsQuery.data ?? [])].sort(
    (a, b) => new Date(a.reservedFor).getTime() - new Date(b.reservedFor).getTime(),
  );

  const freeTables = (tablesQuery.data ?? []).filter((t) => t.tableStatusId === TableStatus.Livre);

  return (
    <main style={{ padding: 22, maxWidth: 1000, margin: "0 auto" }}>
      <div className="rise" style={{ display: "flex", alignItems: "baseline", gap: 14, marginBottom: 6, flexWrap: "wrap" }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>Reservas de mesa</h2>
        <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>próximos 14 dias</span>
        <span style={{ flex: 1 }} />
        <button className="btn-primary" onClick={() => { setError(null); setCreating(true); }}>
          + Nova reserva
        </button>
      </div>

      {reservationsQuery.isError && <QueryError error={reservationsQuery.error} what="as reservas" />}
      {error && !creating && confirmingId === null && <p className="error-text">{error}</p>}

      <div className="rise rise-1" style={{ display: "grid", gap: 10, marginTop: 12 }}>
        {sorted.map((r) => (
          <div key={r.id} className="ticket">
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
                    style={{ minHeight: 36, padding: "0 10px", fontSize: "0.85rem" }}
                    onClick={() => { setError(null); setConfirmingId(r.id); }}
                  >
                    Confirmar
                  </button>
                  <button
                    className="btn-danger"
                    style={{ minHeight: 36, padding: "0 10px", fontSize: "0.85rem" }}
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
                  style={{ minHeight: 36, padding: "0 10px", fontSize: "0.85rem" }}
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
        {sorted.length === 0 && !reservationsQuery.isLoading && (
          <p style={{ color: "var(--ink-faint)" }}>Nenhuma reserva nos próximos 14 dias.</p>
        )}
      </div>

      {creating && (
        <Overlay title="Nova reserva" onClose={() => setCreating(false)}>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Nome do cliente</span>
            <input value={form.customerName} onChange={(e) => setForm((f) => ({ ...f, customerName: e.target.value }))} autoFocus />
          </label>
          <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr 1fr" }}>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Telefone</span>
              <input value={form.customerPhone} onChange={(e) => setForm((f) => ({ ...f, customerPhone: e.target.value }))} />
            </label>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Pessoas</span>
              <input type="number" min={1} value={form.partySize} onChange={(e) => setForm((f) => ({ ...f, partySize: Number(e.target.value) }))} />
            </label>
          </div>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Data e hora</span>
            <input type="datetime-local" value={form.reservedFor} onChange={(e) => setForm((f) => ({ ...f, reservedFor: e.target.value }))} />
          </label>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Observações</span>
            <input value={form.notes} onChange={(e) => setForm((f) => ({ ...f, notes: e.target.value }))} />
          </label>
          {error && <p className="error-text">{error}</p>}
          <button
            className="btn-primary"
            disabled={form.customerName.trim() === "" || form.reservedFor === "" || createMutation.isPending}
            onClick={() => createMutation.mutate()}
          >
            {createMutation.isPending ? "Criando…" : "Criar reserva"}
          </button>
        </Overlay>
      )}

      {confirmingId !== null && (
        <Overlay title="Confirmar reserva — escolher mesa" onClose={() => setConfirmingId(null)}>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Mesa livre</span>
            <select value={tableForConfirm} onChange={(e) => setTableForConfirm(e.target.value)}>
              <option value="">Selecione…</option>
              {freeTables.map((t) => (
                <option key={t.id} value={t.id}>Mesa {t.number}</option>
              ))}
            </select>
          </label>
          {freeTables.length === 0 && (
            <p style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>Nenhuma mesa livre no momento.</p>
          )}
          {error && <p className="error-text">{error}</p>}
          <button
            className="btn-primary"
            disabled={tableForConfirm === "" || confirmMutation.isPending}
            onClick={() => confirmMutation.mutate(confirmingId)}
          >
            {confirmMutation.isPending ? "Confirmando…" : "Confirmar e reservar mesa"}
          </button>
        </Overlay>
      )}
    </main>
  );
}
