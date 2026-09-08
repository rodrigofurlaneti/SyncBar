import { memo } from "react";
import { TableStatus, formatBRL } from "../../lib/types";

const statusColor: Record<number, string> = {
    [TableStatus.Livre]: "var(--free)",
    [TableStatus.Ocupada]: "var(--busy)",
    [TableStatus.Reservada]: "var(--reserved)",
    [TableStatus.EmFechamento]: "var(--closing)",
    [TableStatus.Interditada]: "var(--blocked)",
};

const statusLabel: Record<number, string> = {
    [TableStatus.Livre]: "Livre",
    [TableStatus.Ocupada]: "Ocupada",
    [TableStatus.Reservada]: "Reservada",
    [TableStatus.EmFechamento]: "Fechando",
    [TableStatus.Interditada]: "Interditada",
};

function formatOpenedAt(iso: string): string {
    const date = new Date(iso);
    return `Aberta às ${date.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })}`;
}

// Plural correto pra "1 item" vs "2 itens" — pequeno detalhe que soa errado se hard-coded.
function itemsLabel(count: number): string {
    return `${count} ${count === 1 ? "item" : "itens"}`;
}

export interface TableCardProps {
    id: number;
    number: number;
    statusId: number;
    capacity?: number | null;
    itemsCount?: number;
    totalValue?: number;
    openedAt?: string;
    /** true quando não há pedido nem a mesa está Livre (ex.: Reservada/Interditada sem conta
     * aberta) — nada de útil acontece nesse clique, então desabilita em vez de um clique morto. */
    disabled?: boolean;
    onOpen: (id: number) => void;
}

// <TableCard/> é "burro": só recebe dados já resolvidos (status, contagem de itens, total) e um
// callback de clique — decidir SE o clique deve abrir um pedido novo ou reabrir um existente é
// responsabilidade do componente pai (OrdersPage), que tem acesso ao mapa pedido-por-mesa.
function TableCardComponent({ id, number, statusId, capacity, itemsCount, totalValue, openedAt, disabled, onOpen }: TableCardProps) {
    const isFree = statusId === TableStatus.Livre;
    const isOccupied = itemsCount !== undefined && totalValue !== undefined;
    const color = statusColor[statusId] ?? "var(--ink-faint)";
    const label = statusLabel[statusId] ?? "—";

    // Consolida a informação do cartão numa única frase pra leitor de tela — sem isto, o usuário
    // de teclado/leitor de tela só ouviria "Mesa 4, botão", sem saber se está livre ou ocupada
    // nem o que tem nela, precisando abrir o pedido só pra descobrir.
    const ariaLabel = `Mesa ${number}, ${label}.${
        isOccupied
            ? ` ${itemsLabel(itemsCount!)}, total ${formatBRL(totalValue!)}.`
            : capacity
              ? ` ${capacity} lugares disponíveis.`
              : ""
    }`;

    return (
        <button
            type="button"
            className="table-tile"
            data-testid={`table-tile-${id}`}
            style={{ "--status": color } as React.CSSProperties}
            onClick={() => onOpen(id)}
            disabled={disabled}
            aria-label={ariaLabel}
        >
            <div className="ui-row" style={{ justifyContent: "space-between" }}>
                <span className="num mono-num">{number}</span>
                <span className="chip" style={{ "--dot": color } as React.CSSProperties} aria-hidden="true">
                    {label}
                </span>
            </div>

            <div className="table-tile-body" aria-hidden="true">
                {isOccupied ? (
                    <span className="mono-num">
                        {itemsLabel(itemsCount!)} · {formatBRL(totalValue!)}
                    </span>
                ) : (
                    <span>{capacity ?? "—"} lugares</span>
                )}
                {openedAt && <span className="table-tile-time">{formatOpenedAt(openedAt)}</span>}
            </div>

            {isFree && (
                <span className="btn-ghost btn-sm btn-block table-tile-cta" aria-hidden="true">
                    Abrir mesa
                </span>
            )}
        </button>
    );
}

// A grade pode ter dezenas de mesas re-renderizando a cada poll de 15s (tablesQuery/ordersQuery);
// memo evita recalcular/remontar os cartões cujas props não mudaram entre um refetch e outro.
export const TableCard = memo(TableCardComponent);

// Placeholder de carregamento com a MESMA geometria do cartão real (evita layout shift quando os
// dados chegam) — usa a classe .skeleton já existente no design system.
export function TableCardSkeleton() {
    return (
        <div className="table-tile skeleton" aria-hidden="true" data-testid="table-tile-skeleton" />
    );
}
