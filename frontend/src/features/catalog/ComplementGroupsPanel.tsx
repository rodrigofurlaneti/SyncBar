import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  addComplement,
  createComplementGroup,
  deactivateComplementGroup,
  getComplementGroups,
  getComplementItems,
  removeComplement,
  updateComplementGroup,
  updateComplementPrice,
} from "./complementsApi";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { complementGroupTypeLabel, formatBRL } from "../../lib/types";
import type { ComplementGroupResponse } from "../../lib/types";
import { QueryError } from "../../components/QueryError";
import { Modal } from "../../ui/Modal";
import { Button } from "../../ui/Button";
import { TextField, SelectField } from "../../ui/Field";
import { useToast } from "../../ui/Toast";
import { useDialog } from "../../ui/Dialog";
import { EmptyState } from "../../ui/EmptyState";
import { SkeletonList } from "../../ui/Skeleton";

const emptyForm = { name: "", complementGroupTypeId: "1", minSelection: "0", maxSelection: "1" };
type FormState = typeof emptyForm;

const parseInt0 = (raw: string): number => {
  const value = Number(raw);
  return Number.isFinite(value) && value >= 0 ? Math.trunc(value) : 0;
};

// Fase 6a: cadastro de ComplementGroup (ex.: "Escolha uma bebida", min 1 / max 1) e, dentro dele,
// as opções (Complement) que apontam para um ComplementItem já cadastrado na aba Itens, cada uma
// com seu próprio preço extra (ex.: "Coca-Cola" +R$ 6,00 dentro do grupo).
export function ComplementGroupsPanel() {
  const queryClient = useQueryClient();
  const toast = useToast();
  const dialog = useDialog();
  const { companyId } = useAuthStore();
  const [editing, setEditing] = useState<ComplementGroupResponse | "new" | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [expandedId, setExpandedId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  const groupsQuery = useQuery({
    queryKey: ["complement-groups", companyId],
    queryFn: () => getComplementGroups(companyId ?? 1),
  });

  const itemsQuery = useQuery({
    queryKey: ["complement-items", companyId],
    queryFn: () => getComplementItems(companyId ?? 1),
  });

  const activeItems = useMemo(
    () => (itemsQuery.data ?? []).filter((i) => i.isActive),
    [itemsQuery.data],
  );

  const refresh = () => void queryClient.invalidateQueries({ queryKey: ["complement-groups"] });
  const onApiError = (e: unknown) => setError(e instanceof ApiError ? e.message : "Operação falhou.");

  const openEditor = (group: ComplementGroupResponse | "new") => {
    setError(null);
    setEditing(group);
    setForm(
      group === "new"
        ? emptyForm
        : {
            name: group.name,
            complementGroupTypeId: String(group.complementGroupTypeId),
            minSelection: String(group.minSelection),
            maxSelection: String(group.maxSelection),
          },
    );
  };

  // Duas mutations separadas (em vez de uma só com ternário) porque createComplementGroup
  // retorna Promise<number> e updateComplementGroup retorna Promise<void> — um mutationFn que
  // devolve os dois tipos via ternário não tipa em Promise<number> | Promise<void>, que o
  // useMutation não aceita (mesmo padrão usado em ComplementItemsPanel.tsx).
  const createMutation = useMutation({
    mutationFn: () =>
      createComplementGroup(
        companyId ?? 1,
        form.name.trim(),
        Number(form.complementGroupTypeId),
        parseInt0(form.minSelection),
        parseInt0(form.maxSelection),
      ),
    onSuccess: () => {
      toast.success("Grupo criado.");
      setEditing(null);
      refresh();
    },
    onError: onApiError,
  });

  const updateMutation = useMutation({
    mutationFn: () =>
      updateComplementGroup(
        (editing as ComplementGroupResponse).id,
        form.name.trim(),
        Number(form.complementGroupTypeId),
        parseInt0(form.minSelection),
        parseInt0(form.maxSelection),
      ),
    onSuccess: () => {
      toast.success("Grupo atualizado.");
      setEditing(null);
      refresh();
    },
    onError: onApiError,
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: number) => deactivateComplementGroup(id),
    onSuccess: () => {
      toast.success("Grupo desativado.");
      refresh();
    },
    onError: onApiError,
  });

  const maxBelowMin = parseInt0(form.maxSelection) < parseInt0(form.minSelection);

  return (
    <div style={{ display: "grid", gap: 14 }}>
      <div className="rise" style={{ display: "flex", justifyContent: "flex-end" }}>
        <button type="button" className="btn-primary" onClick={() => openEditor("new")}>
          + Novo grupo
        </button>
      </div>

      {groupsQuery.isError && <QueryError error={groupsQuery.error} what="os grupos de complementos" />}
      {groupsQuery.isLoading && <SkeletonList rows={3} rowHeight={64} />}

      {!groupsQuery.isLoading && (groupsQuery.data?.length ?? 0) === 0 && (
        <EmptyState
          icon="🍔"
          title="Nenhum grupo de complementos"
          description='Crie um grupo (ex.: "Escolha uma bebida") e depois vincule opções da aba Itens a ele.'
          action={
            <button type="button" className="btn-primary" onClick={() => openEditor("new")}>
              + Novo grupo
            </button>
          }
        />
      )}

      {(groupsQuery.data ?? []).map((group) => (
        <GroupCard
          key={group.id}
          group={group}
          expanded={expandedId === group.id}
          onToggle={() => setExpandedId((id) => (id === group.id ? null : group.id))}
          onEdit={() => openEditor(group)}
          onDeactivate={async () => {
            if (
              await dialog.confirm({
                title: "Desativar grupo",
                message: `Desativar "${group.name}"? Produtos vinculados deixam de oferecer este grupo.`,
                confirmLabel: "Desativar",
                danger: true,
              })
            )
              deactivateMutation.mutate(group.id);
          }}
          activeItems={activeItems}
          onError={onApiError}
        />
      ))}

      {editing !== null && (
        <Modal title={editing === "new" ? "Novo grupo de complementos" : "Editar grupo"} onClose={() => setEditing(null)}>
          <TextField
            label="Nome do grupo"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            autoFocus
            placeholder="ex.: Escolha uma bebida"
          />

          <SelectField
            label="Tipo"
            value={form.complementGroupTypeId}
            onChange={(e) => setForm({ ...form, complementGroupTypeId: e.target.value })}
          >
            {Object.entries(complementGroupTypeLabel).map(([id, label]) => (
              <option key={id} value={id}>
                {label}
              </option>
            ))}
          </SelectField>

          <div className="ui-row ui-row-wrap">
            <div style={{ flex: 1, minWidth: 140 }}>
              <TextField
                label="Mínimo de seleções"
                inputMode="numeric"
                value={form.minSelection}
                onChange={(e) => setForm({ ...form, minSelection: e.target.value })}
                hint="0 = grupo opcional"
              />
            </div>
            <div style={{ flex: 1, minWidth: 140 }}>
              <TextField
                label="Máximo de seleções"
                inputMode="numeric"
                value={form.maxSelection}
                onChange={(e) => setForm({ ...form, maxSelection: e.target.value })}
                hint="1 = escolha única (rádio)"
              />
            </div>
          </div>

          {maxBelowMin && (
            <p className="error-text" role="alert">
              O máximo não pode ser menor que o mínimo.
            </p>
          )}
          {error && (
            <p className="error-text" role="alert">
              {error}
            </p>
          )}

          <Button
            variant="primary"
            block
            loading={createMutation.isPending || updateMutation.isPending}
            disabled={form.name.trim() === "" || maxBelowMin}
            onClick={() => (editing === "new" ? createMutation.mutate() : updateMutation.mutate())}
          >
            Salvar
          </Button>
        </Modal>
      )}
    </div>
  );
}

interface GroupCardProps {
  group: ComplementGroupResponse;
  expanded: boolean;
  onToggle: () => void;
  onEdit: () => void;
  onDeactivate: () => void;
  activeItems: { id: number; name: string; isActive: boolean }[];
  onError: (e: unknown) => void;
}

// Cartão expansível — ao abrir, mostra as opções (Complement) já vinculadas ao grupo e um
// formulário para adicionar mais uma opção a partir de um ComplementItem existente.
function GroupCard({ group, expanded, onToggle, onEdit, onDeactivate, activeItems, onError }: GroupCardProps) {
  const queryClient = useQueryClient();
  const toast = useToast();
  const dialog = useDialog();
  const [addItemId, setAddItemId] = useState("");
  const [addPrice, setAddPrice] = useState("");
  const [editingPriceId, setEditingPriceId] = useState<number | null>(null);
  const [editPrice, setEditPrice] = useState("");

  const availableItems = activeItems.filter(
    (item) => !group.complements.some((c) => c.complementItemId === item.id && c.isActive),
  );

  const refresh = () => void queryClient.invalidateQueries({ queryKey: ["complement-groups"] });

  const addMutation = useMutation({
    mutationFn: () => {
      const price = Number(addPrice.replace(",", ".")) || 0;
      return addComplement(group.id, Number(addItemId), price);
    },
    onSuccess: () => {
      toast.success("Opção adicionada ao grupo.");
      setAddItemId("");
      setAddPrice("");
      refresh();
    },
    onError,
  });

  const updatePriceMutation = useMutation({
    mutationFn: (complementId: number) => {
      const price = Number(editPrice.replace(",", ".")) || 0;
      return updateComplementPrice(group.id, complementId, price);
    },
    onSuccess: () => {
      toast.success("Preço atualizado.");
      setEditingPriceId(null);
      refresh();
    },
    onError,
  });

  const removeMutation = useMutation({
    mutationFn: (complementId: number) => removeComplement(group.id, complementId),
    onSuccess: () => {
      toast.success("Opção removida do grupo.");
      refresh();
    },
    onError,
  });

  return (
    <div className="ticket rise">
      <button
        type="button"
        className="ticket-row"
        style={{ width: "100%", background: "transparent", border: "none", cursor: "pointer", textAlign: "left" }}
        onClick={onToggle}
        aria-expanded={expanded}
        aria-controls={`complement-group-panel-${group.id}`}
      >
        <div style={{ display: "grid", gap: 2 }}>
          <span>{group.name}</span>
          <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
            {complementGroupTypeLabel[group.complementGroupTypeId] ?? `Tipo ${group.complementGroupTypeId}`} ·{" "}
            {group.minSelection === 0 ? "opcional" : `mín. ${group.minSelection}`} · máx. {group.maxSelection} ·{" "}
            {group.complements.filter((c) => c.isActive).length} opção(ões)
          </span>
        </div>
        <span style={{ color: "var(--ink-faint)" }}>{expanded ? "▲" : "▼"}</span>
      </button>

      {expanded && (
        <div id={`complement-group-panel-${group.id}`} style={{ padding: "0 16px 14px", display: "grid", gap: 10 }}>
          <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
            <button type="button" className="btn-ghost" style={{ minHeight: 40, padding: "0 12px", fontSize: "0.85rem" }} onClick={onEdit}>
              Editar grupo
            </button>
            <button type="button" className="btn-danger" style={{ minHeight: 40, padding: "0 12px", fontSize: "0.85rem" }} onClick={onDeactivate}>
              Desativar grupo
            </button>
          </div>

          {group.complements.filter((c) => c.isActive).length === 0 && (
            <p style={{ color: "var(--ink-faint)", margin: 0, fontSize: "0.9rem" }}>
              Nenhuma opção neste grupo ainda.
            </p>
          )}

          {group.complements
            .filter((c) => c.isActive)
            .map((c) =>
              editingPriceId === c.id ? (
                <div key={c.id} className="ui-row" style={{ gap: 8 }}>
                  <span style={{ flex: 1 }}>{c.complementItemName}</span>
                  <input
                    autoFocus
                    inputMode="decimal"
                    aria-label={`Preço extra de ${c.complementItemName} em reais`}
                    value={editPrice}
                    onChange={(e) => setEditPrice(e.target.value)}
                    style={{ width: 110 }}
                    onKeyDown={(e) => {
                      if (e.key === "Enter") updatePriceMutation.mutate(c.id);
                      else if (e.key === "Escape") setEditingPriceId(null);
                    }}
                  />
                  <Button size="sm" loading={updatePriceMutation.isPending} onClick={() => updatePriceMutation.mutate(c.id)}>
                    Salvar
                  </Button>
                  <Button size="sm" iconOnly aria-label="Cancelar" onClick={() => setEditingPriceId(null)}>
                    ✕
                  </Button>
                </div>
              ) : (
                <div key={c.id} className="ui-row" style={{ justifyContent: "space-between" }}>
                  <span>{c.complementItemName}</span>
                  <div className="ui-row" style={{ gap: 8 }}>
                    <span className="mono-num" style={{ color: "var(--amber)" }}>
                      {c.extraPrice > 0 ? `+ ${formatBRL(c.extraPrice)}` : "sem custo"}
                    </span>
                    <button
                      type="button"
                      className="btn-ghost"
                      style={{ minHeight: 36, padding: "0 10px", fontSize: "0.8rem" }}
                      onClick={() => {
                        setEditingPriceId(c.id);
                        setEditPrice(String(c.extraPrice));
                      }}
                    >
                      Preço
                    </button>
                    <button
                      type="button"
                      className="btn-danger"
                      style={{ minHeight: 36, padding: "0 10px", fontSize: "0.8rem" }}
                      onClick={async () => {
                        if (
                          await dialog.confirm({
                            title: "Remover opção",
                            message: `Remover "${c.complementItemName}" deste grupo?`,
                            confirmLabel: "Remover",
                            danger: true,
                          })
                        )
                          removeMutation.mutate(c.id);
                      }}
                    >
                      Remover
                    </button>
                  </div>
                </div>
              ),
            )}

          {availableItems.length > 0 ? (
            <div className="ui-row ui-row-wrap" style={{ gap: 8, marginTop: 4 }}>
              <select
                value={addItemId}
                onChange={(e) => setAddItemId(e.target.value)}
                aria-label={`Selecionar complemento para adicionar ao grupo ${group.name}`}
                style={{ flex: 1, minWidth: 160 }}
              >
                <option value="">Adicionar opção…</option>
                {availableItems.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </select>
              <input
                placeholder="Preço extra (R$)…"
                aria-label="Preço extra em reais"
                inputMode="decimal"
                value={addPrice}
                onChange={(e) => setAddPrice(e.target.value)}
                style={{ width: 140 }}
              />
              <Button loading={addMutation.isPending} disabled={addItemId === ""} onClick={() => addMutation.mutate()}>
                Adicionar
              </Button>
            </div>
          ) : (
            <p style={{ color: "var(--ink-faint)", margin: 0, fontSize: "0.85rem" }}>
              Todos os complementos cadastrados já estão neste grupo — cadastre mais na aba Itens.
            </p>
          )}
        </div>
      )}
    </div>
  );
}
