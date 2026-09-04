import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Swal from "sweetalert2"; // Adicionado SweetAlert2
import { addLoyaltyPoints, createCustomer, getCustomersByCompany } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { QueryError } from "../../components/QueryError";
import { Modal } from "../../ui/Modal";
import { Button } from "../../ui/Button";
import { TextField } from "../../ui/Field";
import { EmptyState } from "../../ui/EmptyState";
import { SkeletonList } from "../../ui/Skeleton";

// Configuração base para simular os Toasts no SweetAlert2
const Toast = Swal.mixin({
    toast: true,
    position: "top-end",
    showConfirmButton: false,
    timer: 3000,
    timerProgressBar: true,
});

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

    const onApiError = (e: unknown) => {
        const msg = e instanceof ApiError ? e.message : "Operação falhou.";
        setError(msg);
        Swal.fire("Erro", msg, "error");
    };

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
            Toast.fire({ icon: "success", title: "Cliente cadastrado." });
            refresh();
        },
        onError: onApiError,
    });

    const pointsMutation = useMutation({
        mutationFn: (id: number) => addLoyaltyPoints(id, Number(pointsInput) || 0),
        onSuccess: () => {
            setError(null);
            setAddingPointsTo(null);
            Toast.fire({ icon: "success", title: "Pontos atualizados." });
            refresh();
        },
        onError: onApiError,
    });

    return (
        <main style={{ padding: 22, maxWidth: 900, margin: "0 auto" }}>
            <div className="rise ui-row ui-row-wrap" style={{ marginBottom: 6 }}>
                <h2 className="display" style={{ fontSize: "1.7rem" }}>Clientes</h2>
                <span className="ui-spacer" />
                <input
                    placeholder="Buscar por nome, telefone ou CPF…"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    style={{ flex: 1, minWidth: 220 }}
                    data-testid="input-search-customer"
                />
                <Button variant="primary" onClick={() => { setError(null); setCreating(true); }} data-testid="btn-new-customer">
                    + Novo cliente
                </Button>
            </div>

            {customersQuery.isError && <QueryError error={customersQuery.error} what="os clientes" />}
            {error && !creating && addingPointsTo === null && <p className="error-text" data-testid="error-message">{error}</p>}

            {customersQuery.isLoading && <SkeletonList rows={6} rowHeight={58} />}

            {!customersQuery.isLoading && (customersQuery.data ?? []).length === 0 && (
                <div data-testid="empty-customers-msg">
                    <EmptyState
                        icon="👥"
                        title="Nenhum cliente encontrado"
                        description={
                            search.trim() === ""
                                ? "Cadastre o primeiro cliente para começar o programa de fidelidade."
                                : "Nenhum cliente bate com essa busca."
                        }
                        action={
                            search.trim() === "" ? (
                                <Button variant="primary" onClick={() => { setError(null); setCreating(true); }} data-testid="btn-empty-new-customer">
                                    + Novo cliente
                                </Button>
                            ) : undefined
                        }
                    />
                </div>
            )}

            {!customersQuery.isLoading && (customersQuery.data ?? []).length > 0 && (
                <div className="rise rise-1 ticket" style={{ marginTop: 12 }} data-testid="customers-list">
                    {(customersQuery.data ?? []).map((c) => (
                        <div key={c.id} className="ticket-row" data-testid={`customer-row-${c.id}`}>
                            <div style={{ display: "grid", gap: 2 }}>
                                <span>{c.name}</span>
                                <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                                    {[c.phone, c.cpf, c.email].filter(Boolean).join(" · ") || "sem dados de contato"}
                                </span>
                            </div>
                            <div className="ui-row" style={{ gap: 10 }}>
                                <span className="chip" style={{ "--dot": "var(--amber)" } as React.CSSProperties} data-testid={`points-badge-${c.id}`}>
                                    {c.loyaltyPoints} pts
                                </span>
                                <Button
                                    variant="ghost"
                                    size="sm"
                                    data-testid={`btn-add-points-${c.id}`}
                                    onClick={() => { setError(null); setPointsInput("10"); setAddingPointsTo(c.id); }}
                                >
                                    + Pontos
                                </Button>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {creating && (
                <Modal title="Novo cliente" onClose={() => setCreating(false)}>
                    <TextField
                        label="Nome"
                        value={form.name}
                        onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                        autoFocus
                        data-testid="input-customer-name"
                    />

                    <div className="ui-row ui-row-wrap">
                        <div style={{ flex: 1, minWidth: 140 }}>
                            <TextField
                                label="Telefone"
                                value={form.phone}
                                onChange={(e) => setForm((f) => ({ ...f, phone: e.target.value }))}
                                data-testid="input-customer-phone"
                            />
                        </div>
                        <div style={{ flex: 1, minWidth: 140 }}>
                            <TextField
                                label="CPF"
                                value={form.cpf}
                                onChange={(e) => setForm((f) => ({ ...f, cpf: e.target.value }))}
                                maxLength={11}
                                data-testid="input-customer-cpf"
                            />
                        </div>
                    </div>

                    <TextField
                        label="E-mail"
                        value={form.email}
                        onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
                        data-testid="input-customer-email"
                    />

                    {error && <p className="error-text" data-testid="modal-error-message">{error}</p>}

                    <Button
                        variant="primary"
                        block
                        disabled={form.name.trim() === ""}
                        loading={createMutation.isPending}
                        onClick={() => createMutation.mutate()}
                        data-testid="btn-submit-customer"
                    >
                        Criar cliente
                    </Button>
                </Modal>
            )}

            {addingPointsTo !== null && (
                <Modal title="Adicionar pontos de fidelidade" onClose={() => setAddingPointsTo(null)}>
                    <TextField
                        label="Pontos (use negativo para resgatar)"
                        inputMode="numeric"
                        value={pointsInput}
                        onChange={(e) => setPointsInput(e.target.value)}
                        autoFocus
                        data-testid="input-points"
                    />

                    {error && <p className="error-text" data-testid="points-error-message">{error}</p>}

                    <Button
                        variant="primary"
                        block
                        loading={pointsMutation.isPending}
                        onClick={() => pointsMutation.mutate(addingPointsTo)}
                        data-testid="btn-submit-points"
                    >
                        Aplicar
                    </Button>
                </Modal>
            )}
        </main>
    );
}