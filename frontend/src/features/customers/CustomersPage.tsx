import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { addLoyaltyPoints, createCustomer, getCustomersByCompany } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { QueryError } from "../../components/QueryError";

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
        <main style={{ padding: 22, maxWidth: 900, margin: "0 auto", position: "relative" }}>
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
                <div style={{
                    position: "fixed", top: 0, left: 0, right: 0, bottom: 0,
                    background: "rgba(0,0,0,0.6)", display: "flex", alignItems: "center", justifyContent: "center", zIndex: 1000
                }}>
                    <div style={{ background: "#18181b", padding: 24, borderRadius: 8, width: 450, maxWidth: "90%", display: "grid", gap: 16, border: "1px solid #27272a" }}>
                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                            <h3 style={{ margin: 0, color: "#fff" }}>Novo cliente</h3>
                            <button onClick={() => setCreating(false)} style={{ background: "transparent", border: "none", color: "#fff", cursor: "pointer", fontSize: "1.2rem" }}>✕</button>
                        </div>

                        <label style={{ display: "grid", gap: 4 }}>
                            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Nome</span>
                            <input
                                type="text"
                                value={form.name}
                                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                                autoFocus
                                style={{ padding: "8px", borderRadius: "4px", border: "1px solid #3f3f46", background: "#27272a", color: "#fff" }}
                            />
                        </label>

                        <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr 1fr" }}>
                            <label style={{ display: "grid", gap: 4 }}>
                                <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Telefone</span>
                                <input
                                    type="text"
                                    value={form.phone}
                                    onChange={(e) => setForm((f) => ({ ...f, phone: e.target.value }))}
                                    style={{ padding: "8px", borderRadius: "4px", border: "1px solid #3f3f46", background: "#27272a", color: "#fff" }}
                                />
                            </label>
                            <label style={{ display: "grid", gap: 4 }}>
                                <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>CPF</span>
                                <input
                                    type="text"
                                    value={form.cpf}
                                    onChange={(e) => setForm((f) => ({ ...f, cpf: e.target.value }))}
                                    maxLength={11}
                                    style={{ padding: "8px", borderRadius: "4px", border: "1px solid #3f3f46", background: "#27272a", color: "#fff" }}
                                />
                            </label>
                        </div>

                        <label style={{ display: "grid", gap: 4 }}>
                            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>E-mail</span>
                            <input
                                type="text"
                                value={form.email}
                                onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
                                style={{ padding: "8px", borderRadius: "4px", border: "1px solid #3f3f46", background: "#27272a", color: "#fff" }}
                            />
                        </label>

                        {error && <p className="error-text">{error}</p>}

                        <button
                            className="btn-primary"
                            disabled={form.name.trim() === "" || createMutation.isPending}
                            onClick={() => createMutation.mutate()}
                        >
                            {createMutation.isPending ? "Criando…" : "Criar cliente"}
                        </button>
                    </div>
                </div>
            )}

            {addingPointsTo !== null && (
                <div style={{
                    position: "fixed", top: 0, left: 0, right: 0, bottom: 0,
                    background: "rgba(0,0,0,0.6)", display: "flex", alignItems: "center", justifyContent: "center", zIndex: 1000
                }}>
                    <div style={{ background: "#18181b", padding: 24, borderRadius: 8, width: 400, maxWidth: "90%", display: "grid", gap: 16, border: "1px solid #27272a" }}>
                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                            <h3 style={{ margin: 0, color: "#fff" }}>Adicionar pontos de fidelidade</h3>
                            <button onClick={() => setAddingPointsTo(null)} style={{ background: "transparent", border: "none", color: "#fff", cursor: "pointer", fontSize: "1.2rem" }}>✕</button>
                        </div>

                        <label style={{ display: "grid", gap: 4 }}>
                            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Pontos (use negativo para resgatar)</span>
                            <input
                                inputMode="numeric"
                                value={pointsInput}
                                onChange={(e) => setPointsInput(e.target.value)}
                                autoFocus
                                style={{ padding: "8px", borderRadius: "4px", border: "1px solid #3f3f46", background: "#27272a", color: "#fff" }}
                            />
                        </label>

                        {error && <p className="error-text">{error}</p>}

                        <button
                            className="btn-primary"
                            disabled={pointsMutation.isPending}
                            onClick={() => pointsMutation.mutate(addingPointsTo)}
                        >
                            {pointsMutation.isPending ? "Salvando…" : "Aplicar"}
                        </button>
                    </div>
                </div>
            )}
        </main>
    );
}