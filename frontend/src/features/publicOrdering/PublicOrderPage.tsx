import { useState } from "react";
import { useParams } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import { addPublicOrderItem, getPublicMenu } from "./api";
import { formatBRL } from "../../lib/types";

// Página pública (sem login) acessada via QR Code na mesa — o token na URL é o único
// "segredo": identifica a mesa e a filial. Cada item é enviado ao pedido assim que
// o cliente confirma a quantidade (sem carrinho intermediário — mais simples e robusto
// a perder a aba/recarregar a página no meio do pedido).
export function PublicOrderPage() {
  const { token } = useParams<{ token: string }>();
  const [quantities, setQuantities] = useState<Record<number, number>>({});
  const [sentIds, setSentIds] = useState<number[]>([]);
  const [error, setError] = useState<string | null>(null);

  const menuQuery = useQuery({
    queryKey: ["public-menu", token],
    queryFn: () => getPublicMenu(token!),
    enabled: !!token,
    retry: false,
  });

  const addMutation = useMutation({
    mutationFn: (productId: number) =>
      addPublicOrderItem(token!, productId, quantities[productId] ?? 1, null),
    onSuccess: (_result, productId) => {
      setError(null);
      setSentIds((current) => [...current, productId]);
      setQuantities((current) => ({ ...current, [productId]: 1 }));
    },
    onError: (e) => setError(e instanceof Error ? e.message : "Falha ao enviar o pedido."),
  });

  const setQty = (productId: number, qty: number) =>
    setQuantities((current) => ({ ...current, [productId]: Math.max(1, qty) }));

  if (!token) return null;

  if (menuQuery.isLoading)
    return (
      <main style={{ padding: 24, textAlign: "center", color: "var(--ink-dim)" }}>
        Carregando cardápio…
      </main>
    );

  if (menuQuery.isError)
    return (
      <main style={{ padding: 24, textAlign: "center" }}>
        <p className="error-text">
          Não foi possível carregar o cardápio. Peça a um garçom para gerar um novo QR Code
          para esta mesa.
        </p>
      </main>
    );

  const menu = menuQuery.data!;

  return (
    <main style={{ padding: "20px 16px 60px", maxWidth: 640, margin: "0 auto" }}>
      <div style={{ textAlign: "center", marginBottom: 18 }}>
        <div className="brand" style={{ fontSize: "1.8rem" }}>
          SYNC<em>BAR</em>
        </div>
        <div style={{ color: "var(--ink-dim)", marginTop: 4 }}>
          {menu.branchName} · Mesa {menu.tableNumber}
        </div>
      </div>

      {error && <p className="error-text">{error}</p>}

      <div style={{ display: "grid", gap: 10 }}>
        {menu.items.map((item) => {
          const justSent = sentIds.includes(item.id);
          return (
            <div key={item.id} className="ticket" style={{ padding: "12px 14px" }}>
              <div style={{ display: "flex", justifyContent: "space-between", gap: 10 }}>
                <div style={{ display: "grid", gap: 2 }}>
                  <span>{item.name}</span>
                  {item.description && (
                    <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>{item.description}</span>
                  )}
                  <span className="mono-num" style={{ color: "var(--amber)" }}>{formatBRL(item.salePrice)}</span>
                </div>
                <div style={{ display: "grid", gap: 6, justifyItems: "end" }}>
                  <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
                    <button
                      className="btn-ghost"
                      style={{ minHeight: 32, minWidth: 32, padding: 0 }}
                      onClick={() => setQty(item.id, (quantities[item.id] ?? 1) - 1)}
                    >
                      −
                    </button>
                    <span className="mono-num" style={{ minWidth: 20, textAlign: "center" }}>
                      {quantities[item.id] ?? 1}
                    </span>
                    <button
                      className="btn-ghost"
                      style={{ minHeight: 32, minWidth: 32, padding: 0 }}
                      onClick={() => setQty(item.id, (quantities[item.id] ?? 1) + 1)}
                    >
                      +
                    </button>
                  </div>
                  <button
                    className="btn-primary"
                    style={{ minHeight: 34, padding: "0 14px", fontSize: "0.85rem" }}
                    disabled={addMutation.isPending}
                    onClick={() => addMutation.mutate(item.id)}
                  >
                    {justSent ? "Pedir de novo" : "Pedir"}
                  </button>
                </div>
              </div>
            </div>
          );
        })}
      </div>

      {sentIds.length > 0 && (
        <p style={{ textAlign: "center", color: "var(--ok)", marginTop: 18 }}>
          Pedido enviado para a cozinha/bar. Chame o garçom para fechar a conta quando quiser.
        </p>
      )}
    </main>
  );
}
