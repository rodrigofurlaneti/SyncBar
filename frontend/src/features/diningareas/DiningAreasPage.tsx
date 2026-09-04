import { useState, useMemo } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Swal from "sweetalert2"; // Adicionado SweetAlert2
import {
    createDiningArea,
    getDiningAreasByBranch,
    updateDiningArea,
    getTablesByArea,
    assignTableToArea,
    removeTableFromArea,
    getActiveAssignmentsByArea,
    startAssignment,
    endAssignment,
    type DiningAreaResponse
} from "./api";

import { getEmployeesByBranch } from "../employees/api";
import { getTablesByBranch } from "../tables/api";

import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { QueryError } from "../../components/QueryError";
import { Modal } from "../../ui/Modal";
import { Button } from "../../ui/Button";
import { Field, TextField } from "../../ui/Field";
import { EmptyState } from "../../ui/EmptyState";
import { SkeletonList } from "../../ui/Skeleton";

// Configuração do Toast do SweetAlert2
const Toast = Swal.mixin({
    toast: true,
    position: "top-end",
    showConfirmButton: false,
    timer: 3000,
    timerProgressBar: true,
});

export function DiningAreasPage() {
    const queryClient = useQueryClient();
    const { branchId } = useAuthStore();

    const [search, setSearch] = useState("");
    const [creating, setCreating] = useState(false);
    const [editingTo, setEditingTo] = useState<{ id: number; name: string } | null>(null);
    const [managingArea, setManagingArea] = useState<DiningAreaResponse | null>(null);
    const [error, setError] = useState<string | null>(null);

    const [form, setForm] = useState({ name: "" });
    const [tableForm, setTableForm] = useState({ tableId: "" });
    const [assignmentForm, setAssignmentForm] = useState({ employeeId: "" });

    const onApiError = (e: unknown) => {
        const msg = e instanceof ApiError ? e.message : "Operação falhou.";
        setError(msg);
        Swal.fire("Erro", msg, "error");
    };

    const refreshAreas = () => void queryClient.invalidateQueries({ queryKey: ["diningareas"] });
    const refreshManagement = () => {
        if (managingArea) {
            void queryClient.invalidateQueries({ queryKey: ["diningareatables", managingArea.id] });
            void queryClient.invalidateQueries({ queryKey: ["diningareaassignments", managingArea.id] });
        }
    };

    const areasQuery = useQuery({
        queryKey: ["diningareas", branchId],
        queryFn: () => getDiningAreasByBranch(branchId ?? 1),
        enabled: !!branchId,
    });

    const branchTablesQuery = useQuery({
        queryKey: ["tables", branchId],
        queryFn: () => getTablesByBranch(branchId ?? 1),
        enabled: !!branchId && managingArea !== null,
    });

    const employeesQuery = useQuery({
        queryKey: ["employees", branchId],
        queryFn: () => getEmployeesByBranch(branchId ?? 1),
        enabled: !!branchId && managingArea !== null,
    });

    const areaTablesQuery = useQuery({
        queryKey: ["diningareatables", managingArea?.id],
        queryFn: () => getTablesByArea(managingArea!.id),
        enabled: !!managingArea,
    });

    const assignmentsQuery = useQuery({
        queryKey: ["diningareaassignments", managingArea?.id],
        queryFn: () => getActiveAssignmentsByArea(managingArea!.id),
        enabled: !!managingArea,
    });

    const employeeNameMap = useMemo(() => {
        const map = new Map<number, string>();
        for (const e of employeesQuery.data ?? []) map.set(e.id, e.name);
        return map;
    }, [employeesQuery.data]);

    const tableNumberMap = useMemo(() => {
        const map = new Map<number, number>();
        for (const t of branchTablesQuery.data ?? []) map.set(t.id, t.number);
        return map;
    }, [branchTablesQuery.data]);

    const createMutation = useMutation({
        mutationFn: () => createDiningArea({ branchId: branchId ?? 1, name: form.name.trim() }),
        onSuccess: () => {
            setError(null); setCreating(false); setForm({ name: "" });
            Toast.fire({ icon: "success", title: "Praça cadastrada." });
            refreshAreas();
        },
        onError: onApiError,
    });

    const updateMutation = useMutation({
        mutationFn: () => updateDiningArea(editingTo!.id, editingTo!.name.trim()),
        onSuccess: () => {
            setError(null); setEditingTo(null);
            Toast.fire({ icon: "success", title: "Praça atualizada." });
            refreshAreas();
        },
        onError: onApiError,
    });

    const addTableMutation = useMutation({
        mutationFn: () => assignTableToArea(managingArea!.id, Number(tableForm.tableId)),
        onSuccess: () => {
            setError(null); setTableForm({ tableId: "" });
            Toast.fire({ icon: "success", title: "Mesa vinculada à praça." });
            refreshManagement();
        },
        onError: onApiError,
    });

    const removeTableMutation = useMutation({
        mutationFn: (assignmentId: number) => removeTableFromArea(assignmentId),
        onSuccess: () => {
            Toast.fire({ icon: "success", title: "Mesa removida da praça." });
            refreshManagement();
        },
        onError: onApiError,
    });

    const startAssignmentMutation = useMutation({
        mutationFn: () => startAssignment(managingArea!.id, Number(assignmentForm.employeeId), new Date().toISOString()),
        onSuccess: () => {
            setError(null); setAssignmentForm({ employeeId: "" });
            Toast.fire({ icon: "success", title: "Turno do garçom iniciado na praça." });
            refreshManagement();
        },
        onError: onApiError,
    });

    const endAssignmentMutation = useMutation({
        mutationFn: (assignmentId: number) => endAssignment(assignmentId, new Date().toISOString()),
        onSuccess: () => {
            Toast.fire({ icon: "success", title: "Turno encerrado." });
            refreshManagement();
        },
        onError: onApiError,
    });

    const filteredAreas = (areasQuery.data ?? []).filter((area) =>
        area.name.toLowerCase().includes(search.toLowerCase())
    );

    return (
        <main style={{ padding: 22, maxWidth: 900, margin: "0 auto" }}>
            <div className="rise ui-row ui-row-wrap" style={{ marginBottom: 6 }}>
                <h2 className="display" style={{ fontSize: "1.7rem" }}>Praças e Salões</h2>
                <span className="ui-spacer" />
                <input
                    placeholder="Buscar praça…"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    style={{ flex: 1, minWidth: 220 }}
                    data-testid="input-search-area"
                />
                <Button variant="primary" onClick={() => { setError(null); setCreating(true); }} data-testid="btn-new-area">
                    + Nova praça
                </Button>
            </div>

            {areasQuery.isError && <QueryError error={areasQuery.error} what="as praças" />}
            {error && !creating && editingTo === null && managingArea === null && <p className="error-text" data-testid="error-message-main">{error}</p>}

            {areasQuery.isLoading && <SkeletonList rows={4} rowHeight={58} />}

            {!areasQuery.isLoading && filteredAreas.length === 0 && (
                <div data-testid="empty-areas-msg">
                    <EmptyState
                        icon="🍽️"
                        title="Nenhuma praça encontrada"
                        description={
                            search.trim() === ""
                                ? "Cadastre a primeira praça (ex: Salão Interno, Varanda) para organizar suas mesas."
                                : "Nenhuma praça bate com essa busca."
                        }
                        action={
                            search.trim() === "" ? (
                                <Button variant="primary" onClick={() => { setError(null); setCreating(true); }} data-testid="btn-empty-new-area">
                                    + Nova praça
                                </Button>
                            ) : undefined
                        }
                    />
                </div>
            )}

            {!areasQuery.isLoading && filteredAreas.length > 0 && (
                <div className="rise rise-1 ticket" style={{ marginTop: 12 }} data-testid="areas-list">
                    {filteredAreas.map((area) => (
                        <div key={area.id} className="ticket-row" data-testid={`area-row-${area.id}`}>
                            <div style={{ display: "grid", gap: 2 }}>
                                <span style={{ fontWeight: 600 }}>{area.name}</span>
                                <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                                    {area.isActive ? "Ativa" : "Inativa"}
                                </span>
                            </div>
                            <div className="ui-row" style={{ gap: 10 }}>
                                <Button variant="ghost" size="sm" onClick={() => { setError(null); setEditingTo({ id: area.id, name: area.name }); }} data-testid={`btn-edit-area-${area.id}`}>
                                    Editar Nome
                                </Button>
                                <Button variant="primary" size="sm" onClick={() => { setError(null); setManagingArea(area); }} data-testid={`btn-manage-area-${area.id}`}>
                                    Gerenciar Operação
                                </Button>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {creating && (
                <Modal title="Nova praça" onClose={() => setCreating(false)}>
                    <TextField
                        label="Nome (ex: Varanda, Salão 1)"
                        value={form.name}
                        onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                        autoFocus
                        data-testid="input-area-name"
                    />
                    {error && <p className="error-text" data-testid="modal-error-message">{error}</p>}
                    <Button
                        variant="primary"
                        block
                        disabled={form.name.trim() === ""}
                        loading={createMutation.isPending}
                        onClick={() => createMutation.mutate()}
                        data-testid="btn-submit-area"
                    >
                        Criar praça
                    </Button>
                </Modal>
            )}

            {editingTo !== null && (
                <Modal title="Editar praça" onClose={() => setEditingTo(null)}>
                    <TextField
                        label="Nome"
                        value={editingTo.name}
                        onChange={(e) => setEditingTo((prev) => prev ? { ...prev, name: e.target.value } : null)}
                        autoFocus
                        data-testid="input-edit-area-name"
                    />
                    {error && <p className="error-text">{error}</p>}
                    <Button
                        variant="primary"
                        block
                        disabled={editingTo.name.trim() === ""}
                        loading={updateMutation.isPending}
                        onClick={() => updateMutation.mutate()}
                        data-testid="btn-submit-edit-area"
                    >
                        Salvar alterações
                    </Button>
                </Modal>
            )}

            {managingArea !== null && (
                <Modal title={`Gerenciando: ${managingArea.name}`} onClose={() => { setManagingArea(null); setError(null); }}>
                    {error && <p className="error-text" style={{ marginBottom: 16 }}>{error}</p>}

                    <div style={{ marginBottom: 24 }}>
                        <h3 style={{ fontSize: "1.1rem", marginBottom: 8 }}>Mesas Vinculadas</h3>
                        <div className="ui-row" style={{ gap: 8, marginBottom: 12, alignItems: "end" }}>
                            <div style={{ flex: 1 }}>
                                <Field label="Selecione a Mesa">
                                    {(a11y) => (
                                        <select
                                            {...a11y}
                                            value={tableForm.tableId}
                                            onChange={(e) => setTableForm({ tableId: e.target.value })}
                                            data-testid="select-table"
                                        >
                                            <option value="">Escolha uma mesa…</option>
                                            {(branchTablesQuery.data ?? []).map((t) => (
                                                <option key={t.id} value={t.id}>Mesa {t.number}</option>
                                            ))}
                                        </select>
                                    )}
                                </Field>
                            </div>
                            <Button
                                variant="ghost"
                                disabled={!tableForm.tableId}
                                loading={addTableMutation.isPending}
                                onClick={() => addTableMutation.mutate()}
                                data-testid="btn-link-table"
                            >
                                + Vincular
                            </Button>
                        </div>

                        {areaTablesQuery.isLoading ? <p>Carregando mesas...</p> : (
                            <ul style={{ paddingLeft: 20 }}>
                                {areaTablesQuery.data?.map(t => (
                                    <li key={t.id} style={{ marginBottom: 4 }} data-testid={`linked-table-${t.id}`}>
                                        Mesa {tableNumberMap.get(t.diningTableId) ?? t.diningTableId}
                                        <button
                                            style={{ marginLeft: 8, color: "var(--red)", background: "none", border: "none", cursor: "pointer" }}
                                            data-testid={`btn-remove-table-${t.id}`}
                                            onClick={async () => {
                                                const { isConfirmed } = await Swal.fire({
                                                    title: "Remover mesa?",
                                                    text: "Deseja desvincular esta mesa da praça?",
                                                    icon: "warning",
                                                    showCancelButton: true,
                                                    confirmButtonText: "Remover",
                                                    cancelButtonText: "Cancelar"
                                                });
                                                if (isConfirmed) removeTableMutation.mutate(t.id);
                                            }}
                                        >
                                            (remover)
                                        </button>
                                    </li>
                                ))}
                                {areaTablesQuery.data?.length === 0 && <li style={{ color: "var(--ink-faint)" }}>Nenhuma mesa vinculada a esta praça.</li>}
                            </ul>
                        )}
                    </div>

                    <hr style={{ border: "0", borderTop: "1px solid var(--border)", margin: "16px 0" }} />

                    <div>
                        <h3 style={{ fontSize: "1.1rem", marginBottom: 8 }}>Garçons no Turno</h3>
                        <div className="ui-row" style={{ gap: 8, marginBottom: 12, alignItems: "end" }}>
                            <div style={{ flex: 1 }}>
                                <Field label="Selecione o Garçom">
                                    {(a11y) => (
                                        <select
                                            {...a11y}
                                            value={assignmentForm.employeeId}
                                            onChange={(e) => setAssignmentForm({ employeeId: e.target.value })}
                                            data-testid="select-employee"
                                        >
                                            <option value="">Escolha um garçom…</option>
                                            {(employeesQuery.data ?? []).map((e) => (
                                                <option key={e.id} value={e.id}>{e.name}</option>
                                            ))}
                                        </select>
                                    )}
                                </Field>
                            </div>
                            <Button
                                variant="ghost"
                                disabled={!assignmentForm.employeeId}
                                loading={startAssignmentMutation.isPending}
                                onClick={() => startAssignmentMutation.mutate()}
                                data-testid="btn-start-shift"
                            >
                                + Iniciar Turno
                            </Button>
                        </div>

                        {assignmentsQuery.isLoading ? <p>Carregando escala...</p> : (
                            <ul style={{ paddingLeft: 20 }}>
                                {assignmentsQuery.data?.map(a => (
                                    <li key={a.id} style={{ marginBottom: 4 }} data-testid={`active-shift-${a.id}`}>
                                        <span style={{ fontWeight: 500 }}>{employeeNameMap.get(a.employeeId) ?? a.employeeId}</span>
                                        <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)", marginLeft: 6 }}>
                                            (iniciou às {new Date(a.startAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })})
                                        </span>
                                        <button
                                            style={{ marginLeft: 8, color: "var(--red)", background: "none", border: "none", cursor: "pointer" }}
                                            data-testid={`btn-end-shift-${a.id}`}
                                            onClick={async () => {
                                                const { isConfirmed } = await Swal.fire({
                                                    title: "Encerrar turno?",
                                                    text: "O garçom não receberá mais pedidos desta praça.",
                                                    icon: "question",
                                                    showCancelButton: true,
                                                    confirmButtonText: "Encerrar",
                                                    cancelButtonText: "Cancelar"
                                                });
                                                if (isConfirmed) endAssignmentMutation.mutate(a.id);
                                            }}
                                        >
                                            (encerrar turno)
                                        </button>
                                    </li>
                                ))}
                                {assignmentsQuery.data?.length === 0 && <li style={{ color: "var(--ink-faint)" }}>Nenhum garçom alocado nesta praça.</li>}
                            </ul>
                        )}
                    </div>
                </Modal>
            )}
        </main>
    );
}