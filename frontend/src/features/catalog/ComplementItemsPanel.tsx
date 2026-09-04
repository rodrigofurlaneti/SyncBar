import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Swal from "sweetalert2"; // Adicionado SweetAlert2
import {
    createComplementItem,
    deactivateComplementItem,
    getComplementItems,
    updateComplementItem,
} from "./complementsApi";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import type { ComplementItemResponse } from "../../lib/types";
import { QueryError } from "../../components/QueryError";
import { Button } from "../../ui/Button";
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

export function ComplementItemsPanel() {
    const queryClient = useQueryClient();
    const { companyId } = useAuthStore();
    const [newName, setNewName] = useState("");
    const [editing, setEditing] = useState<ComplementItemResponse | null>(null);
    const [editName, setEditName] = useState("");
    const [error, setError] = useState<string | null>(null);

    const itemsQuery = useQuery({
        queryKey: ["complement-items", companyId],
        queryFn: () => getComplementItems(companyId ?? 1),
    });

    const refresh = () => void queryClient.invalidateQueries({ queryKey: ["complement-items"] });

    const onApiError = (e: unknown) => {
        const msg = e instanceof ApiError ? e.message : "Operação falhou.";
        setError(msg);
        Swal.fire("Erro", msg, "error");
    };

    const createMutation = useMutation({
        mutationFn: () => createComplementItem(companyId ?? 1, newName.trim()),
        onSuccess: () => {
            Toast.fire({ icon: "success", title: "Complemento criado." });
            setNewName("");
            setError(null);
            refresh();
        },
        onError: onApiError,
    });

    const updateMutation = useMutation({
        mutationFn: () => updateComplementItem(editing!.id, editName.trim()),
        onSuccess: () => {
            Toast.fire({ icon: "success", title: "Complemento atualizado." });
            setEditing(null);
            refresh();
        },
        onError: onApiError,
    });

    const deactivateMutation = useMutation({
        mutationFn: (id: number) => deactivateComplementItem(id),
        onSuccess: () => {
            Toast.fire({ icon: "success", title: "Complemento desativado." });
            refresh();
        },
        onError: onApiError,
    });

    return (
        <div style={{ display: "grid", gap: 14 }}>
            <div className="rise" style={{ display: "flex", gap: 8, maxWidth: 460 }}>
                <input
                    placeholder="Nome do complemento (ex.: Bacon, Coca-Cola)…"
                    value={newName}
                    onChange={(e) => setNewName(e.target.value)}
                    data-testid="input-new-item"
                    onKeyDown={(e) => {
                        if (e.key === "Enter" && newName.trim() !== "") createMutation.mutate();
                    }}
                />
                <Button
                    loading={createMutation.isPending}
                    disabled={newName.trim() === ""}
                    onClick={() => createMutation.mutate()}
                    data-testid="btn-create-item"
                >
                    + Criar
                </Button>
            </div>

            {error && !editing && (
                <p className="error-text" role="alert" data-testid="error-message">
                    {error}
                </p>
            )}
            {itemsQuery.isError && <QueryError error={itemsQuery.error} what="os complementos" />}
            {itemsQuery.isLoading && <SkeletonList rows={4} rowHeight={52} />}

            {!itemsQuery.isLoading && (itemsQuery.data?.length ?? 0) === 0 && (
                <div data-testid="empty-items-msg">
                    <EmptyState
                        icon="🧂"
                        title="Nenhum complemento cadastrado"
                        description="Cadastre as opções (ex.: Bacon, Coca-Cola, Sem cebola) que depois entram em um grupo, na aba Grupos."
                    />
                </div>
            )}

            {(itemsQuery.data?.length ?? 0) > 0 && (
                <div className="ticket" data-testid="items-list">
                    {(itemsQuery.data ?? []).map((item) =>
                        editing?.id === item.id ? (
                            <div className="ticket-row" key={item.id} style={{ gap: 8 }} data-testid={`editing-row-${item.id}`}>
                                <input
                                    autoFocus
                                    value={editName}
                                    onChange={(e) => setEditName(e.target.value)}
                                    style={{ flex: 1 }}
                                    data-testid={`input-edit-item-${item.id}`}
                                    onKeyDown={(e) => {
                                        if (e.key === "Enter" && editName.trim() !== "") updateMutation.mutate();
                                        else if (e.key === "Escape") setEditing(null);
                                    }}
                                />
                                <Button
                                    size="sm"
                                    loading={updateMutation.isPending}
                                    disabled={editName.trim() === ""}
                                    onClick={() => updateMutation.mutate()}
                                    data-testid={`btn-save-edit-${item.id}`}
                                >
                                    Salvar
                                </Button>
                                <Button
                                    size="sm"
                                    iconOnly
                                    aria-label="Cancelar edição"
                                    onClick={() => setEditing(null)}
                                    data-testid={`btn-cancel-edit-${item.id}`}
                                >
                                    ✕
                                </Button>
                            </div>
                        ) : (
                            <div className="ticket-row" key={item.id} data-testid={`item-row-${item.id}`}>
                                <span>{item.name}</span>
                                <div style={{ display: "flex", gap: 8 }}>
                                    <button
                                        type="button"
                                        className="btn-ghost"
                                        style={{ minHeight: 44, padding: "0 12px", fontSize: "0.85rem" }}
                                        data-testid={`btn-edit-item-${item.id}`}
                                        onClick={() => {
                                            setEditing(item);
                                            setEditName(item.name);
                                        }}
                                    >
                                        Editar
                                    </button>
                                    <button
                                        type="button"
                                        className="btn-danger"
                                        style={{ minHeight: 44, padding: "0 12px", fontSize: "0.85rem" }}
                                        data-testid={`btn-deactivate-item-${item.id}`}
                                        onClick={async () => {
                                            const { isConfirmed } = await Swal.fire({
                                                title: "Desativar complemento",
                                                text: `Desativar "${item.name}"? Ele deixa de poder ser adicionado a novos grupos.`,
                                                icon: "warning",
                                                showCancelButton: true,
                                                confirmButtonColor: "#d33",
                                                confirmButtonText: "Desativar",
                                                cancelButtonText: "Cancelar"
                                            });

                                            if (isConfirmed) {
                                                deactivateMutation.mutate(item.id);
                                            }
                                        }}
                                    >
                                        Desativar
                                    </button>
                                </div>
                            </div>
                        ),
                    )}
                </div>
            )}
        </div>
    );
}