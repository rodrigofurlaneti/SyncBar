import { useState } from "react";
import { Modal } from "../../ui/Modal";
import { Button } from "../../ui/Button";
import { formatBRL } from "../../lib/types";
import type { ComplementGroupResponse, OrderItemComplementSelection } from "../../lib/types";

interface Props {
  productName: string;
  groups: ComplementGroupResponse[];
  onCancel: () => void;
  onConfirm: (selections: OrderItemComplementSelection[]) => void;
  confirmLabel?: string;
  submitting?: boolean;
}

// Fase 6a: aberto antes de lançar um item que tem grupos de complementos vinculados (balcão/mesa
// via OrderDrawer, autoatendimento QR Code via PublicOrderPage — mesmo componente nos dois, só
// muda quem chama). minSelection/maxSelection de cada grupo vêm do cadastro (ComplementsPage);
// o preço nunca é escolhido aqui, só exibido — quem resolve o ExtraPrice de verdade é o backend.
export function ComplementSelectorModal({
  productName,
  groups,
  onCancel,
  onConfirm,
  confirmLabel = "Adicionar",
  submitting = false,
}: Props) {
  const [selected, setSelected] = useState<Record<number, number[]>>({});

  const toggle = (group: ComplementGroupResponse, complementId: number) => {
    setSelected((current) => {
      const chosen = current[group.id] ?? [];
      const isSingle = group.maxSelection <= 1;

      if (chosen.includes(complementId)) {
        return { ...current, [group.id]: chosen.filter((id) => id !== complementId) };
      }
      if (isSingle) return { ...current, [group.id]: [complementId] };
      if (chosen.length >= group.maxSelection) return current; // já atingiu o máximo do grupo
      return { ...current, [group.id]: [...chosen, complementId] };
    });
  };

  const countFor = (group: ComplementGroupResponse) => (selected[group.id] ?? []).length;
  const groupSatisfied = (group: ComplementGroupResponse) => {
    const count = countFor(group);
    return count >= group.minSelection && count <= group.maxSelection;
  };
  const allSatisfied = groups.every(groupSatisfied);

  const handleConfirm = () => {
    const selections: OrderItemComplementSelection[] = groups.flatMap((group) =>
      (selected[group.id] ?? []).map((complementId) => ({ complementGroupId: group.id, complementId })),
    );
    onConfirm(selections);
  };

  return (
    <Modal title={`Complementos — ${productName}`} onClose={onCancel}>
      <div style={{ display: "grid", gap: 18 }}>
        {groups.map((group) => {
          const chosen = selected[group.id] ?? [];
          const isSingle = group.maxSelection <= 1;
          const satisfied = groupSatisfied(group);
          const activeComplements = group.complements.filter((c) => c.isActive);

          return (
            <div key={group.id} style={{ display: "grid", gap: 8 }}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline" }}>
                <span style={{ fontWeight: 600 }}>{group.name}</span>
                <span style={{ fontSize: "0.8rem", color: satisfied ? "var(--ink-faint)" : "var(--danger)" }}>
                  {group.minSelection === 0
                    ? `opcional · até ${group.maxSelection}`
                    : group.minSelection === group.maxSelection
                      ? `escolha ${group.minSelection}`
                      : `escolha de ${group.minSelection} a ${group.maxSelection}`}
                </span>
              </div>

              {activeComplements.length === 0 && (
                <span style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
                  Nenhuma opção ativa neste grupo.
                </span>
              )}

              <div style={{ display: "grid", gap: 6 }}>
                {activeComplements.map((c) => {
                  const isChosen = chosen.includes(c.id);
                  return (
                    <label
                      key={c.id}
                      className="ui-row"
                      style={{
                        justifyContent: "space-between",
                        padding: "8px 10px",
                        borderRadius: 8,
                        border: `1px solid ${isChosen ? "var(--amber)" : "var(--line)"}`,
                        cursor: "pointer",
                      }}
                    >
                      <span className="ui-row" style={{ gap: 8 }}>
                        <input
                          type={isSingle ? "radio" : "checkbox"}
                          name={`complement-group-${group.id}`}
                          checked={isChosen}
                          onChange={() => toggle(group, c.id)}
                        />
                        {c.complementItemName}
                      </span>
                      <span className="mono-num" style={{ color: "var(--ink-faint)" }}>
                        {c.extraPrice > 0 ? `+ ${formatBRL(c.extraPrice)}` : "sem custo"}
                      </span>
                    </label>
                  );
                })}
              </div>
            </div>
          );
        })}

        <Button variant="primary" block loading={submitting} disabled={!allSatisfied} onClick={handleConfirm}>
          {confirmLabel}
        </Button>
      </div>
    </Modal>
  );
}
