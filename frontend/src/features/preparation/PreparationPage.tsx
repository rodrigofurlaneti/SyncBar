import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { advanceItemStatus, getPreparationQueue } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { OrderItemStatus, orderItemStatusLabel } from "../../lib/types";
import type { PreparationItemResponse } from "../../lib/types";
import { QueryError } from "../../components/QueryError";

// Verde ate 70% do tempo-limite, amarelo ate 100%, vermelho (pulsando) estourado.
function timeColor(ratio: number): string {
  if (ratio >= 1) return "var(--danger)";
  if (ratio >= 0.7) return "var(--busy)";
  return "var(--free)";
}

const nextStatus: Record<number, { id: number; label: string }> = {
  [OrderItemStatus.Lancado]: { id: OrderItemStatus.EmPreparo, label: "Iniciar" },
  [OrderItemStatus.EnviadoCozinha]: { id: OrderItemStatus.EmPreparo, label: "Iniciar" },
  [OrderItemStatus.EmPreparo]: { id: OrderItemStatus.Pronto, label: "Pronto" },
  [OrderItemStatus.Pronto]: { id: OrderItemStatus.Entregue, label: "Entregue" },
};

function formatElapsed(ms: number): string {
  const totalSeconds = Math.max(0, Math.floor(ms / 1000));
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
}

function ItemRow({
  item,
  now,
  onAdvance,
  isPending,
}: {
  item: PreparationItemResponse;
  now: number;
  onAdvance: (statusId: number) => void;
  isPending: boolean;
}) {
  const elapsedMs = now - new Date(item.startedAt).getTime();
  const limitMs = item.limitMinutes * 60_000;
  const ratio = limitMs <= 0 ? 1 : elapsedMs / limitMs;
  const color = timeColor(ratio);
  const next = nextStatus[item.orderItemStatusId];
  const overdue = ratio >= 1;

  return (
    <div
      className={overdue ? "kds-overdue" : undefined}
      style={{
        display: "grid",
        gap: 6,
        padding: "10px 12px",
        borderBottom: "1px solid var(--line-soft)",
      }}
    >
      <div style={{ display: "flex", justifyContent: "space-between", gap: 8, alignItems: "baseline" }}>
        <span style={{ fontWeight: 600 }}>
          {item.quantity} × {item.productName}
          {item.isBarItem && (
            <span
              style={{
                marginLeft: 8,
                fontFamily: "var(--font-cond)",
                fontSize: "0.7rem",
                letterSpacing: "0.1em",
                color: "var(--reserved)",
                border: "1px solid var(--reserved)",
                borderRadius: 4,
                padding: "1px 6px",
                verticalAlign: "middle",
              }}
            >
              BAR
            </span>
          )}
        </span>
        <span className="mono-num display" style={{ fontSize: "1.15rem", color }}>
          {formatElapsed(elapsedMs)}
        </span>
      </div>

      {item.notes && (
        <span style={{ fontSize: "0.82rem", color: "var(--amber)" }}>“{item.notes}”</span>
      )}

      <div style={{ background: "var(--bg-press)", borderRadius: 999, height: 8, overflow: "hidden" }}>
        <div
          style={{
            width: `${Math.min(100, ratio * 100)}%`,
            height: "100%",
            background: color,
            transition: "width 1s linear, background 300ms ease",
          }}
        />
      </div>

      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 8 }}>
        <span style={{ fontSize: "0.78rem", color: "var(--ink-faint)" }}>
          {orderItemStatusLabel[item.orderItemStatusId]} · limite {item.limitMinutes} min
        </span>
        {next && (
          <button
            className="btn-primary"
            style={{ minHeight: 40, padding: "0 16px", fontSize: "0.9rem" }}
            disabled={isPending}
            onClick={() => onAdvance(next.id)}
          >
            {next.label}
          </button>
        )}
      </div>
    </div>
  );
}

export function PreparationPage() {
  const queryClient = useQueryClient();
  const { branchId } = useAuthStore();
  const [now, setNow] = useState(() => Date.now());

  // Relogio local (1s) + atualizacao da fila (10s).
  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, []);

  const queueQuery = useQuery({
    queryKey: ["preparation", branchId],
    queryFn: () => getPreparationQueue(branchId),
    refetchInterval: 10_000,
  });

  const advanceMutation = useMutation({
    mutationFn: ({ orderId, itemId, statusId }: { orderId: number; itemId: number; statusId: number }) =>
      advanceItemStatus(orderId, itemId, statusId),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["preparation"] }),
  });

  const tickets = queueQuery.data ?? [];

  return (
    <main style={{ padding: 22 }}>
      <div className="rise" style={{ display: "flex", alignItems: "baseline", gap: 14, marginBottom: 6 }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>Preparo</h2>
        <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
          verde no prazo · amarelo atenção · vermelho estourou o tempo
        </span>
      </div>

      {queueQuery.isError && <QueryError error={queueQuery.error} what="a fila de preparo" />}

      {tickets.length === 0 && !queueQuery.isLoading && (
        <div style={{ display: "grid", placeItems: "center", minHeight: "50vh" }}>
          <div style={{ textAlign: "center", display: "grid", gap: 6 }}>
            <span className="display" style={{ fontSize: "2.2rem", color: "var(--free)" }}>
              Tudo em dia
            </span>
            <span style={{ color: "var(--ink-faint)" }}>Nenhum item aguardando preparo.</span>
          </div>
        </div>
      )}

      {/* Regua de pedidos: trilho horizontal, ticket mais antigo primeiro */}
      <div
        className="rise rise-1"
        style={{
          display: "flex",
          gap: 14,
          overflowX: "auto",
          paddingBottom: 14,
          alignItems: "flex-start",
        }}
      >
        {tickets.map((ticket) => {
          const worstRatio = Math.max(
            ...ticket.items.map((item) => {
              const elapsed = now - new Date(item.startedAt).getTime();
              return elapsed / (item.limitMinutes * 60_000);
            }),
          );
          const headColor = timeColor(worstRatio);
          return (
            <div
              key={ticket.customerOrderId}
              className="ticket"
              style={{ minWidth: 300, maxWidth: 340, flexShrink: 0 }}
            >
              <div
                className="ticket-head"
                style={{ borderTop: `3px solid ${headColor}`, alignItems: "center" }}
              >
                <span className="display" style={{ fontSize: "1.35rem" }}>
                  {ticket.tableNumber !== null
                    ? `Mesa ${ticket.tableNumber}`
                    : `Comanda ${ticket.comandaCode ?? "?"}`}
                </span>
                <span className="mono-num" style={{ color: "var(--ink-faint)", fontSize: "0.8rem" }}>
                  #{ticket.customerOrderId} ·{" "}
                  {new Date(ticket.openedAt).toLocaleTimeString("pt-BR", {
                    hour: "2-digit",
                    minute: "2-digit",
                  })}
                </span>
              </div>
              {ticket.items.map((item) => (
                <ItemRow
                  key={item.orderItemId}
                  item={item}
                  now={now}
                  isPending={advanceMutation.isPending}
                  onAdvance={(statusId) =>
                    advanceMutation.mutate({
                      orderId: ticket.customerOrderId,
                      itemId: item.orderItemId,
                      statusId,
                    })
                  }
                />
              ))}
            </div>
          );
        })}
      </div>
    </main>
  );
}
