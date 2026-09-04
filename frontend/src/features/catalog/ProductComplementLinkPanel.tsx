import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Swal from "sweetalert2"; // Adicionado SweetAlert2
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

interface Props {
    productId: number;
}

// Configuração base para simular os Toasts no SweetAlert2
const Toast = Swal.mixin({
    toast: true,
    position: "top-end",
    showConfirmButton: false,
    timer: 3000,
    timerProgressBar: true,
});

export function ProductComplementLinkPanel({ productId }: Props) {
    const queryClient = useQueryClient();
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

    const onApiError = (e: unknown) => {
        const msg = e instanceof ApiError ? e.message : "Operação falhou.";
        setError(msg);
        Swal.fire("Erro", msg, "error");
    };

    const linkMutation = useMutation({
        mutationFn: () =>
            linkProductComplementGroup(productId, Number(addGroupId), (linksQuery.data?.length ?? 0) + 1),
        onSuccess: () => {
            Toast.fire({ icon: "success", title: "Grupo vinculado ao produto." });
            setAddGroupId("");
            setError(null);
            refresh();
        },
        onError: onApiError,
    });

    const unlinkMutation = useMutation({
        mutationFn: (productComplementGroupId: number) => unlinkProductComplementGroup(productComplementGroupId),
        onSuccess: () => {
            Toast.fire({ icon: "success", title: "Grupo desvinculado." });
            refresh();
        },
        onError: onApiError,
    });

    const linkedGroupIds = new Set((linksQuery.data ?? []).map((l) => l.complementGroupId));
    const availableGroups = (allGroupsQuery.data ?? []).filter(
        (g) => g.isActive && !linkedGroupIds.has(g.id),
    );

    return (
        <div className="field" style={{ gap: 8 }} data-testid="product-complement-link-panel">
            <span className="field-label">Grupos de complementos deste produto</span>

            {linksQuery.isLoading && (
                <span style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>Carregando…</span>
            )}

            {!linksQuery.isLoading && (linksQuery.data?.length ?? 0) === 0 && (
                <span style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }} data-testid="empty-links-msg">
                    Nenhum grupo vinculado — o produto não terá complementos ao ser lançado num pedido.
                </span>
            )}

            {(linksQuery.data ?? []).map((link) => (
                <div
                    key={link.productComplementGroupId}
                    className="ui-row"
                    style={{ justifyContent: "space-between", padding: "6px 0", borderBottom: "1px solid var(--line)" }}
                    data-testid={`linked-group-${link.productComplementGroupId}`}
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
                        data-testid={`btn-unlink-group-${link.productComplementGroupId}`}
                        onClick={async () => {
                            const { isConfirmed } = await Swal.fire({
                                title: "Desvincular grupo",
                                text: `Remover "${link.complementGroupName}" deste produto?`,
                                icon: "warning",
                                showCancelButton: true,
                                confirmButtonColor: "#d33",
                                confirmButtonText: "Remover",
                                cancelButtonText: "Cancelar"
                            });

                            if (isConfirmed) {
                                unlinkMutation.mutate(link.productComplementGroupId);
                            }
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
                        data-testid="select-add-group"
                    >
                        <option value="">Vincular grupo…</option>
                        {availableGroups.map((g) => (
                            <option key={g.id} value={g.id}>
                                {g.name}
                            </option>
                        ))}
                    </select>
                    <Button
                        loading={linkMutation.isPending}
                        disabled={addGroupId === ""}
                        onClick={() => linkMutation.mutate()}
                        data-testid="btn-link-group"
                    >
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
                <p className="error-text" role="alert" data-testid="error-message">
                    {error}
                </p>
            )}
        </div>
    );
}