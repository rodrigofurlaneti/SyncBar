import { memo } from "react";
import { ComandaStatus, formatBRL } from "../../lib/types";
import { OrderCardSummary, itemsLabel, formatOpenedAt } from "./OrderCardSummary";

const comandaColor: Record<number, string> = {
    [ComandaStatus.Disponivel]: "var(--free)",
    [ComandaStatus.EmUso]: "var(--busy)",
    [ComandaStatus.Extraviada]: "var(--closing)",
    [ComandaStatus.Bloqueada]: "var(--blocked)",
};

const comandaStatusLabel: Record<number, string> = {
    [ComandaStatus.Disponivel]: "Livre",
    [ComandaStatus.EmUso]: "Em uso",
    [ComandaStatus.Extraviada]: "Extraviada",
    [ComandaStatus.Bloqueada]: "Bloqueada",
};

export interface ComandaCardProps {
    id: number;
    code: string;
    statusId: number;
    totalValue?: number;
    itemsCount?: number;
    openedAt?: string;
    /** true quando não há pedido nem a comanda está Disponível (Extraviada/Bloqueada). */
    disabled?: boolean;
    onOpen: (id: number) => void;
}

// Mesmo princípio do <TableCard/>: componente burro, só formata o que recebe.
function ComandaCardComponent({ id, code, statusId, totalValue, itemsCount, openedAt, disabled, onOpen }: ComandaCardProps) {
    const isBusy = statusId === ComandaStatus.EmUso;
    const color = comandaColor[statusId] ?? "var(--ink-faint)";
    const label = comandaStatusLabel[statusId] ?? "—";

    const ariaLabel = `Comanda ${code}, ${label}.${
        totalValue !== undefined ? ` ${itemsLabel(itemsCount ?? 0)}, total ${formatBRL(totalValue)}. ${formatOpenedAt(openedAt)}.` : ""
    }`;

    return (
        <button
            type="button"
            className={`comanda-tile${isBusy ? " is-busy" : ""}`}
            data-testid={`comanda-tile-${id}`}
            style={{ "--status": color } as React.CSSProperties}
            onClick={() => onOpen(id)}
            disabled={disabled}
            aria-label={ariaLabel}
        >
            <span className="comanda-tile-code mono-num" aria-hidden="true">{code}</span>
            {totalValue !== undefined ? (
                <OrderCardSummary itemsCount={itemsCount ?? 0} totalValue={totalValue} openedAt={openedAt} />
            ) : (
                <span className="comanda-tile-status" style={{ color }} aria-hidden="true">{label}</span>
            )}
        </button>
    );
}

export const ComandaCard = memo(ComandaCardComponent);

export function ComandaCardSkeleton() {
    return <div className="comanda-tile skeleton" aria-hidden="true" data-testid="comanda-tile-skeleton" />;
}
