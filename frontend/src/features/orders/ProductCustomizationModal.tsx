import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "../../lib/apiClient";
import { Modal } from "../../ui/Modal";
import { Button } from "../../ui/Button";
import { formatBRL } from "../../lib/types";
import type { MenuItemResponse, OrderItemComplementSelection } from "../../lib/types";
import { ComplementSelectorModal } from "./ComplementSelectorModal";

type ProductDetails = {
    salePrice: number;
    hasOptionalExtras: boolean;
    hasBoosts: boolean;
    optionalExtras: { id: number; optionalExtraName: string; displayOrder: number }[];
    boosts: { id: number; boostName: string; incrementalValue: number; displayOrder: number }[];
};

export function ProductCustomizationModal({ product, onCancel, onConfirm, submitting, error, discountRate = 0 }: {
    product: MenuItemResponse;
    onCancel: () => void;
    onConfirm: (complements: OrderItemComplementSelection[], optionalExtraIds: number[], boostIds: number[]) => void;
    submitting: boolean;
    error: string | null;
    discountRate?: number;
}) {
    const [optionalIds, setOptionalIds] = useState<number[]>([]);
    const [boostIds, setBoostIds] = useState<number[]>([]);
    const configurable = !!(product.hasOptionalExtras || product.hasBoosts);
    const details = useQuery({
        queryKey: ["product-customization", product.id],
        queryFn: () => api<ProductDetails>(`/api/products/${product.id}`),
        enabled: configurable,
        retry: false,
        staleTime: 0,
        gcTime: 0,
    });
    if (configurable && (details.isPending || details.isError)) return (
        <Modal title={`Personalizar — ${product.name}`} onClose={onCancel}>
            {details.isPending ? <p role="status">Carregando opções…</p> : <>
                <p role="alert">Não foi possível carregar as opções deste produto.</p>
                <Button onClick={() => void details.refetch()}>Tentar novamente</Button>
            </>}
        </Modal>
    );
    const optionalExtras = details.data?.hasOptionalExtras ? details.data.optionalExtras : [];
    const boosts = details.data?.hasBoosts ? details.data.boosts : [];
    const toggle = (ids: number[], id: number) => ids.includes(id) ? ids.filter(x => x !== id) : [...ids, id];
    return (
        <ComplementSelectorModal productName={product.name} groups={product.complementGroups}
            onCancel={onCancel} submitting={submitting} error={error} confirmLabel="Adicionar item"
            basePrice={Math.round((details.data?.salePrice ?? product.salePrice) * (1 - discountRate) * 100) / 100}
            additionalPrice={boosts.filter(x => boostIds.includes(x.id)).reduce((sum, x) => sum + x.incrementalValue, 0)}
            onConfirm={complements => onConfirm(complements,
                optionalIds.filter(id => optionalExtras.some(x => x.id === id)),
                boostIds.filter(id => boosts.some(x => x.id === id)))}>
            {optionalExtras.length > 0 && <fieldset disabled={submitting}>
                <legend>Opcionais gratuitos</legend>
                {optionalExtras.map(item => <label className="ui-row" key={item.id}>
                    <input type="checkbox" checked={optionalIds.includes(item.id)}
                        onChange={() => setOptionalIds(ids => toggle(ids, item.id))} />
                    {item.optionalExtraName} (grátis)
                </label>)}
            </fieldset>}
            {boosts.length > 0 && <fieldset disabled={submitting}>
                <legend>Adicionais pagos</legend>
                {boosts.map(item => <label className="ui-row" key={item.id}>
                    <input type="checkbox" checked={boostIds.includes(item.id)}
                        onChange={() => setBoostIds(ids => toggle(ids, item.id))} />
                    {item.boostName} (+ {formatBRL(item.incrementalValue)})
                </label>)}
            </fieldset>}
            {configurable && optionalExtras.length === 0 && boosts.length === 0 &&
                <p>Nenhum opcional ou adicional disponível. Você pode adicionar o produto sem essas opções.</p>}
        </ComplementSelectorModal>
    );
}
