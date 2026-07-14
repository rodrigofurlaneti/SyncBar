import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { addLoyaltyPoints, createCustomer, getCustomersByCompany } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { QueryError } from "../../components/QueryError";
import { Overlay } from "../orders/Overlay";

export function CustomersPage() {
  const queryClient = useQueryClient();
  const { companyId } = useAuthStore();
  const [search, setSearch] = useState("");
  const [creating, setCreating] = useState(false);
  const [addingPointsTo, setAddingPointsTo] = useState<number | null>(null);
  const [pointsInput, setPointsInput] = useState("10");
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState({ name: "", phone: "", cpf: "", email: "" });

  const customersQuery = useQuery({
    queryKey: ["customers", companyId, search],
    queryFn: () => getCustomersByCompany(companyId ?? 1, search),
  });

  const refresh = () => void queryClient.invalidateQueries({ queryKey: ["customers"] });
  const onApiError = (e: unknown) => setError(e instanceof ApiError ? e.message : "Operação falhou.");

  const createMutation = useMutation({
    mutationFn: () =>
      createCustomer({
        companyId: companyId ?? 1,
        name: form.name.trim(),
        phone: form.phone.trim() === "" ? null : form.phone.trim(),
        cpf: form.cpf.trim() === "" ? null : form.cpf.trim(),
        email: form.email.trim() === "" ? null : form.email.trim(),
      }),
    onSuccess: () => {
      setError(null);
      setCreating(false);
      setForm({ name: "", phone: "", cpf: "", email: "" });
      refresh();
    },
    onError: onApiError,
  });

  const pointsMutation = useMutation({
    mutationFn: (id: number) => addLoyaltyPoints(id, Number(pointsInput) || 0),
    onSuccess: () => {
      setError(null);
      setAddingPointsTo(null);
      refresh();
    },
    onError: onApiError,
  });

  return (
    <main style={{ padding: 22, maxWidth: 900, margin: "0 auto" }}>
      <div className="rise" style={{ display: "flex", alignItems: "baseline", gap: 14, marginBottom: 6, flexWrap: "wrap" }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>Clientes</h2>
        <span style={{ flex: 1 }} />
        <input
          placeholder="Buscar por nome, telefone ou CPF…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          style={{ width: 260 }}
        />
        <button className="btn-primary" onClick={() => { setError(null); setCreating(true); }}>
          + Novo cliente
        </button>
      </div>

      {customersQuery.isError && <QueryError error={customersQuery.error} what="os clientes" />}
      {error && !creating && addingPointsTo === null && <p className="error-text">{error}</p>}

      <div className="rise rise-1" style={{ display: "grid", gap: 8, marginTop: 12 }}>
        {(customersQuery.data ?? []).map((c) => (
          <div key={c.id} className="ticket-row">
            <div style={{ display: "grid", gap: 2 }}>
              <span>{c.name}</span>
              <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                {[c.phone, c.cpf, c.email].filter(Boolean).join(" · ") || "sem dados de contato"}
              </span>
            </div>
            <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
              <span className="chip" style={{ "--dot": "var(--amber)" } as React.CSSProperties}>
                {c.loyaltyPoints} pts
              </span>
              <button
                className="btn-ghost"
                style={{ minHeight: 36, padding: "0 10px", fontSize: "0.85rem" }}
                onClick={() => { setError(null); setPointsInput("10"); setAddingPointsTo(c.id); }}
              >
                + Pontos
              </button>
            </div>
          </div>
        ))}
        {(customersQuery.data ?? []).length === 0 && !customersQuery.isLoading && (
          <p style={{ color: "var(--ink-faint)" }}>Nenhum cliente encontrado.</p>
        )}
      </div>

      {creating && (
        <Overlay title="Novo cliente" onClose={() => setCreating(false)}>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Nome</span>
            <input value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} autoFocus />
          </label>
          <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr 1fr" }}>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Telefone</span>
              <input value={form.phone} onChange={(e) => setForm((f) => ({ ...f, phone: e.target.value }))} />
            </label>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>CPF</span>
              <input value={form.cpf} onChange={(e) => setForm((f) => ({ ...f, cpf: e.target.value }))} maxLength={11} />
            </label>
          </div>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>E-mail</span>
            <input value={form.email} onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))} />
          </label>
          {error && <p className="error-text">{error}</p>}
          <button
            className="btn-primary"
            disabled={form.name.trim() === "" || createMutation.isPending}
            onClick={() => createMutation.mutate()}
          >
            {createMutation.isPending ? "Criando…" : "Criar cliente"}
          </button>
        </Overlay>
      )}

      {addingPointsTo !== null && (
        <Overlay title="Adicionar pontos de fidelidade" onClose={() => setAddingPointsTo(null)}>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Pontos (use negativo para resgatar)</span>
            <input inputMode="numeric" value={pointsInput} onChange={(e) => setPointsInput(e.target.value)} autoFocus />
          </label>
          {error && <p className="error-text">{error}</p>}
          <button
            className="btn-primary"
            disabled={pointsMutation.isPending}
            onClick={() => pointsMutation.mutate(addingPointsTo)}
          >
            {pointsMutation.isPending ? "Salvando…" : "Aplicar"}
          </button>
        </Overlay>
      )}
    </main>
  );
}
