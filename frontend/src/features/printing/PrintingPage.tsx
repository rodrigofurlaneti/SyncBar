import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createPrinter,
  deactivatePrinter,
  getPrinters,
  getPrintSettings,
  setPrintSettings,
  testPrinter,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { QueryError } from "../../components/QueryError";
import { Overlay } from "../orders/Overlay";

const emptyForm = {
  name: "",
  connectionType: 1,
  printerName: "ELGIN i9(USB)",
  ipAddress: "",
  port: "9100",
  printsOrders: false,
  printsBills: false,
};

export function PrintingPage() {
  const queryClient = useQueryClient();
  const { branchId } = useAuthStore();
  const [creating, setCreating] = useState(false);
  const [form, setForm] = useState(emptyForm);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  const settingsQuery = useQuery({
    queryKey: ["printing", "settings", branchId],
    queryFn: () => getPrintSettings(branchId),
  });

  const printersQuery = useQuery({
    queryKey: ["printing", "printers", branchId],
    queryFn: () => getPrinters(branchId),
  });

  const refresh = () => void queryClient.invalidateQueries({ queryKey: ["printing"] });
  const onApiError = (e: unknown) =>
    setError(e instanceof ApiError ? e.message : "Operação falhou.");

  const settingsMutation = useMutation({
    mutationFn: (next: { printOrdersEnabled: boolean; printBillsEnabled: boolean }) =>
      setPrintSettings({ branchId, ...next }),
    onSuccess: () => {
      setError(null);
      refresh();
    },
    onError: onApiError,
  });

  const createMutation = useMutation({
    mutationFn: () =>
      createPrinter({
        branchId,
        name: form.name.trim(),
        connectionType: form.connectionType,
        printerName: form.connectionType === 1 ? form.printerName.trim() : null,
        ipAddress: form.connectionType === 2 ? form.ipAddress.trim() : null,
        port: form.connectionType === 2 ? Number(form.port) : null,
        printsOrders: form.printsOrders,
        printsBills: form.printsBills,
      }),
    onSuccess: () => {
      setError(null);
      setCreating(false);
      setForm(emptyForm);
      refresh();
    },
    onError: onApiError,
  });

  const removeMutation = useMutation({
    mutationFn: (id: number) => deactivatePrinter(id),
    onSuccess: refresh,
    onError: onApiError,
  });

  const testMutation = useMutation({
    mutationFn: (id: number) => testPrinter(id),
    onSuccess: () => {
      setError(null);
      setFeedback("Cupom de teste enviado — confira a impressora.");
      setTimeout(() => setFeedback(null), 4000);
    },
    onError: onApiError,
  });

  const settings = settingsQuery.data;

  const Toggle = ({ label, hint, value, onChange }: {
    label: string; hint: string; value: boolean; onChange: (v: boolean) => void;
  }) => (
    <label className="ticket" style={{ padding: 16, display: "flex", gap: 14, alignItems: "center", cursor: "pointer" }}>
      <input
        type="checkbox"
        style={{ width: 26, minHeight: 26 }}
        checked={value}
        onChange={(e) => onChange(e.target.checked)}
        disabled={settingsMutation.isPending}
      />
      <span style={{ display: "grid", gap: 2 }}>
        <strong>{label}</strong>
        <span style={{ fontSize: "0.82rem", color: "var(--ink-faint)" }}>{hint}</span>
      </span>
    </label>
  );

  return (
    <main style={{ padding: 22, maxWidth: 900, margin: "0 auto" }}>
      <div className="rise" style={{ display: "flex", alignItems: "baseline", gap: 14, marginBottom: 16 }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>Impressão</h2>
        <span style={{ flex: 1 }} />
        <button className="btn-primary" onClick={() => { setError(null); setCreating(true); }}>
          + Nova impressora
        </button>
      </div>

      {settingsQuery.isError && <QueryError error={settingsQuery.error} what="as configurações" />}
      {printersQuery.isError && <QueryError error={printersQuery.error} what="as impressoras" />}
      {error && !creating && <p className="error-text">{error}</p>}
      {feedback && <p style={{ color: "var(--ok)" }}>{feedback}</p>}

      {settings && (
        <div className="rise rise-1" style={{ display: "grid", gap: 10, gridTemplateColumns: "1fr 1fr", marginBottom: 18 }}>
          <Toggle
            label="Imprimir pedidos"
            hint="Cupom automático na cozinha/bar a cada lançamento"
            value={settings.printOrdersEnabled}
            onChange={(v) => settingsMutation.mutate({ printOrdersEnabled: v, printBillsEnabled: settings.printBillsEnabled })}
          />
          <Toggle
            label="Imprimir contas"
            hint="Habilita o 'Deseja imprimir?' no fechamento e o cupom de caixa"
            value={settings.printBillsEnabled}
            onChange={(v) => settingsMutation.mutate({ printOrdersEnabled: settings.printOrdersEnabled, printBillsEnabled: v })}
          />
        </div>
      )}

      <div className="ticket rise rise-2">
        <div className="ticket-head">
          <span className="display" style={{ fontSize: "1.2rem" }}>Impressoras</span>
        </div>
        {(printersQuery.data ?? []).map((printer) => (
          <div className="ticket-row" key={printer.id} style={{ flexWrap: "wrap", gap: 8 }}>
            <div style={{ display: "grid", gap: 2 }}>
              <span>{printer.name}</span>
              <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                {printer.connectionType === 1
                  ? `USB/Windows · driver "${printer.printerName}"`
                  : `Rede · ${printer.ipAddress}:${printer.port}`}
                {" · "}
                {[printer.printsOrders ? "pedidos" : null, printer.printsBills ? "contas/fechamentos" : null]
                  .filter(Boolean)
                  .join(" + ")}
              </span>
            </div>
            <div style={{ display: "flex", gap: 8, marginLeft: "auto" }}>
              <button
                className="btn-ghost"
                style={{ minHeight: 38, padding: "0 12px", fontSize: "0.85rem" }}
                disabled={testMutation.isPending}
                onClick={() => testMutation.mutate(printer.id)}
              >
                Testar
              </button>
              <button
                className="btn-danger"
                style={{ minHeight: 38, padding: "0 12px", fontSize: "0.85rem" }}
                onClick={() => {
                  if (window.confirm(`Remover a impressora "${printer.name}"?`))
                    removeMutation.mutate(printer.id);
                }}
              >
                Remover
              </button>
            </div>
          </div>
        ))}
        {printersQuery.data?.length === 0 && (
          <div className="ticket-row" style={{ color: "var(--ink-faint)" }}>
            Nenhuma impressora cadastrada.
          </div>
        )}
      </div>

      {creating && (
        <Overlay title="Nova impressora" onClose={() => setCreating(false)}>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Nome (identificação)</span>
            <input placeholder="ex.: Cozinha, Bar, Caixa" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          </label>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Conexão</span>
            <select
              value={form.connectionType}
              onChange={(e) => setForm({ ...form, connectionType: Number(e.target.value) })}
            >
              <option value={1}>USB — driver instalado no Windows</option>
              <option value={2}>Rede — TCP (porta 9100)</option>
            </select>
          </label>
          {form.connectionType === 1 ? (
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>
                Nome exato do driver (Configurações do Windows → Impressoras)
              </span>
              <input value={form.printerName} onChange={(e) => setForm({ ...form, printerName: e.target.value })} />
            </label>
          ) : (
            <div style={{ display: "grid", gap: 8, gridTemplateColumns: "2fr 1fr" }}>
              <label style={{ display: "grid", gap: 4 }}>
                <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Endereço IP</span>
                <input placeholder="192.168.0.50" value={form.ipAddress} onChange={(e) => setForm({ ...form, ipAddress: e.target.value })} />
              </label>
              <label style={{ display: "grid", gap: 4 }}>
                <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Porta</span>
                <input inputMode="numeric" value={form.port} onChange={(e) => setForm({ ...form, port: e.target.value })} />
              </label>
            </div>
          )}
          <div style={{ display: "grid", gap: 6 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>O que esta impressora imprime?</span>
            <label style={{ display: "flex", gap: 10, alignItems: "center" }}>
              <input type="checkbox" style={{ width: 20, minHeight: 20 }} checked={form.printsOrders}
                onChange={(e) => setForm({ ...form, printsOrders: e.target.checked })} />
              <span>Pedidos (cozinha/bar)</span>
            </label>
            <label style={{ display: "flex", gap: 10, alignItems: "center" }}>
              <input type="checkbox" style={{ width: 20, minHeight: 20 }} checked={form.printsBills}
                onChange={(e) => setForm({ ...form, printsBills: e.target.checked })} />
              <span>Contas e fechamentos (caixa)</span>
            </label>
          </div>
          {error && <p className="error-text">{error}</p>}
          <button
            className="btn-primary"
            disabled={
              form.name.trim() === "" ||
              (!form.printsOrders && !form.printsBills) ||
              (form.connectionType === 1 && form.printerName.trim() === "") ||
              (form.connectionType === 2 && (form.ipAddress.trim() === "" || form.port.trim() === "")) ||
              createMutation.isPending
            }
            onClick={() => createMutation.mutate()}
          >
            {createMutation.isPending ? "Salvando…" : "Salvar impressora"}
          </button>
        </Overlay>
      )}
    </main>
  );
}
