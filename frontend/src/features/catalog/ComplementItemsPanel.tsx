import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
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
import { useToast } from "../../ui/Toast";
import { useDialog } from "../../ui/Dialog";
import { EmptyState } from "../../ui/EmptyState";
import { SkeletonList } from "../../ui/Skeleton";

// Fase 6a: cadastro leve de ComplementItem (ex.: "Coca-Cola", "Bacon") — reutilizado dentro de
// vários ComplementGroup (ver ComplementGroupsPanel), igual um Product é reutilizado em vários
// pedidos.
export function ComplementItemsPanel() {
  const queryClient = useQueryClient();
  const toast = useToast();
  const dialog = useDialog();
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
  const onApiError = (e: unknown) => setError(e instanceof ApiError ? e.message : "Operação falhou.");

  const createMutation = useMutation({
    mutationFn: () => createComplementItem(companyId ?? 1, newName.trim()),
    onSuccess: () => {
      toast.success("Complemento criado.");
      setNewName("");
      setError(null);
      refresh();
    },
    onError: onApiError,
  });

  const updateMutation = useMutation({
    mutationFn: () => updateComplementItem(editing!.id, editName.trim()),
    onSuccess: () => {
      toast.success("Complemento atualizado.");
      setEditing(null);
      refresh();
    },
    onError: onApiError,
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: number) => deactivateComplementItem(id),
    onSuccess: () => {
      toast.success("Complemento desativado.");
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
          onKeyDown={(e) => {
            if (e.key === "Enter" && newName.trim() !== "") createMutation.mutate();
          }}
        />
        <Button
          loading={createMutation.isPending}
          disabled={newName.trim() === ""}
          onClick={() => createMutation.mutate()}
        >
          + Criar
        </Button>
      </div>

      {error && !editing && <p className="error-text">{error}</p>}
      {itemsQuery.isError && <QueryError error={itemsQuery.error} what="os complementos" />}
      {itemsQuery.isLoading && <SkeletonList rows={4} rowHeight={52} />}

      {!itemsQuery.isLoading && (itemsQuery.data?.length ?? 0) === 0 && (
        <EmptyState
          icon="🧂"
          title="Nenhum complemento cadastrado"
          description="Cadastre as opções (ex.: Bacon, Coca-Cola, Sem cebola) que depois entram em um grupo, na aba Grupos."
        />
      )}

      {(itemsQuery.data?.length ?? 0) > 0 && (
        <div className="ticket">
          {(itemsQuery.data ?? []).map((item) =>
            editing?.id === item.id ? (
              <div className="ticket-row" key={item.id} style={{ gap: 8 }}>
                <input
                  autoFocus
                  value={editName}
                  onChange={(e) => setEditName(e.target.value)}
                  style={{ flex: 1 }}
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
                >
                  Salvar
                </Button>
                <Button size="sm" iconOnly aria-label="Cancelar edição" onClick={() => setEditing(null)}>
                  ✕
                </Button>
              </div>
            ) : (
              <div className="ticket-row" key={item.id}>
                <span>{item.name}</span>
                <div style={{ display: "flex", gap: 8 }}>
                  <button
                    className="btn-ghost"
                    style={{ minHeight: 44, padding: "0 12px", fontSize: "0.85rem" }}
                    onClick={() => {
                      setEditing(item);
                      setEditName(item.name);
                    }}
                  >
                    Editar
                  </button>
                  <button
                    className="btn-danger"
                    style={{ minHeight: 44, padding: "0 12px", fontSize: "0.85rem" }}
                    onClick={async () => {
                      if (
                        await dialog.confirm({
                          title: "Desativar complemento",
                          message: `Desativar "${item.name}"? Ele deixa de poder ser adicionado a novos grupos.`,
                          confirmLabel: "Desativar",
                          danger: true,
                        })
                      )
                        deactivateMutation.mutate(item.id);
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
