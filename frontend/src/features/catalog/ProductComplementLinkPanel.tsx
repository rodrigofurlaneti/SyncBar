import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  getComplementGroups,
  getProductComplementGroups,
  linkProductComplementGroup,
  unlinkProductComplementGroup,
} from "./complementsApi";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { complementGroupTypeLabel } from "../../lib/types";
import { Button } from "../../ui/Button";
import { useToast } from "../../ui/Toast";
import { useDialog } from "../../ui/Dialog";

interface Props {
  productId: number;
}

// Fase 6a: painel embutido no formulário de edição de produto (ProductsPage) — vincula/desvincula
// ComplementGroup ao produto (ProductComplementGroup), na ordem em que devem aparecer pro cliente.
// Só aparece editando um produto já existente (precisa de productId — não faz sentido em "novo").
export function ProductComplementLinkPanel({ productId }: Props) {
  const queryClient = useQueryClient();
  const toast = useToast();
  const dialog = useDialog();
  const { companyId } = useAuthStore();
  const [addGroupId, setAddGroupId] = useState("");
  const [error, setError] = useState<string | null>(null);

  const linksQuery = useQuery({
    queryKey: ["product-complement-groups", productId],
    queryFn: () => getProductComplementGroups(productId),
  });

  const allGroupsQuery = useQuery({
    queryKey: ["complement-groups", companyId],
    queryFn: () => getComplementGroups(companyId ?? 1),
  });

  const refresh = () => void queryClient.invalidateQueries({ queryKey: ["product-complement-groups", productId] });
  const onApiError = (e: unknown) => setError(e instanceof ApiError ? e.message : "Operação falhou.");

  const linkMutation = useMutation({
    mutationFn: () =>
      linkProductComplementGroup(productId, Number(addGroupId), (linksQuery.data?.length ?? 0) + 1),
    onSuccess: () => {
      toast.success("Grupo vinculado ao produto.");
      setAddGroupId("");
      setError(null);
      refresh();
    },
    onError: onApiError,
  });

  const unlinkMutation = useMutation({
    mutationFn: (productComplementGroupId: number) => unlinkProductComplementGroup(productComplementGroupId),
    onSuccess: () => {
      toast.success("Grupo desvinculado.");
      refresh();
    },
    onError: onApiError,
  });

  const linkedGroupIds = new Set((linksQuery.data ?? []).map((l) => l.complementGroupId));
  const availableGroups = (allGroupsQuery.data ?? []).filter(
    (g) => g.isActive && !linkedGroupIds.has(g.id),
  );

  return (
    <div className="field" style={{ gap: 8 }}>
      <span className="field-label">Grupos de complementos deste produto</span>

      {linksQuery.isLoading && (
        <span style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>Carregando…</span>
      )}

      {!linksQuery.isLoading && (linksQuery.data?.length ?? 0) === 0 && (
        <span style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
          Nenhum grupo vinculado — o produto não terá complementos ao ser lançado num pedido.
        </span>
      )}

      {(linksQuery.data ?? []).map((link) => (
        <div
          key={link.productComplementGroupId}
          className="ui-row"
          style={{ justifyContent: "space-between", padding: "6px 0", borderBottom: "1px solid var(--line)" }}
        >
          <span>
            {link.complementGroupName}{" "}
            <span style={{ color: "var(--ink-faint)", fontSize: "0.8rem" }}>
              ({complementGroupTypeLabel[link.complementGroupTypeId] ?? `Tipo ${link.complementGroupTypeId}`}
              {link.minSelection === 0 ? ", opcional" : `, mín. ${link.minSelection}`}, máx. {link.maxSelection})
            </span>
          </span>
          <Button
            size="sm"
            variant="danger"
            loading={unlinkMutation.isPending}
            onClick={async () => {
              if (
                await dialog.confirm({
                  title: "Desvincular grupo",
                  message: `Remover "${link.complementGroupName}" deste produto?`,
                  confirmLabel: "Remover",
                  danger: true,
                })
              )
                unlinkMutation.mutate(link.productComplementGroupId);
            }}
          >
            Remover
          </Button>
        </div>
      ))}

      {availableGroups.length > 0 ? (
        <div className="ui-row ui-row-wrap" style={{ gap: 8, marginTop: 6 }}>
          <select
            value={addGroupId}
            onChange={(e) => setAddGroupId(e.target.value)}
            aria-label="Selecionar grupo de complementos para vincular ao produto"
            style={{ flex: 1, minWidth: 160 }}
          >
            <option value="">Vincular grupo…</option>
            {availableGroups.map((g) => (
              <option key={g.id} value={g.id}>
                {g.name}
              </option>
            ))}
          </select>
          <Button loading={linkMutation.isPending} disabled={addGroupId === ""} onClick={() => linkMutation.mutate()}>
            Vincular
          </Button>
        </div>
      ) : (
        allGroupsQuery.data !== undefined && (
          <span style={{ color: "var(--ink-faint)", fontSize: "0.8rem", marginTop: 4 }}>
            {(allGroupsQuery.data ?? []).length === 0
              ? "Nenhum grupo cadastrado ainda — crie um na tela Complementos."
              : "Todos os grupos já estão vinculados a este produto."}
          </span>
        )
      )}

      {error && (
        <p className="error-text" role="alert">
          {error}
        </p>
      )}
    </div>
  );
}
