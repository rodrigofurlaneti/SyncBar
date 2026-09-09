import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "../../lib/apiClient";
import type { MenuItemResponse, OrderItemComplementSelection } from "../../lib/types";
import { complementSteps } from "./ComplementSelectorModal";
import { ProductWizard } from "./ProductWizard";
import type { WizardStep } from "./useProductWizard";

export type ProductDetails = {
    salePrice: number;
    hasOptionalExtras: boolean;
    hasBoosts: boolean;
    optionalExtras: { id: number; optionalExtraName: string; displayOrder: number }[];
    boosts: { id: number; boostName: string; incrementalValue: number; displayOrder: number }[];
};

export function ProductCustomizationModal({ product, onCancel, onConfirm, submitting, error, discountRate = 0, embeddedDetails, quantity = 1 }: {
    product: MenuItemResponse;
    onCancel: () => void;
    onConfirm: (complements: OrderItemComplementSelection[], optionalExtraIds: number[], boostIds: number[]) => void;
    submitting: boolean;
    error: string | null;
    discountRate?: number;
    embeddedDetails?: ProductDetails;
    quantity?: number;
}) {
    const configurable = !!(product.hasOptionalExtras || product.hasBoosts);
    const details = useQuery({
        queryKey: ["product-customization", product.id],
        queryFn: () => api<ProductDetails>(`/api/products/${product.id}`),
        enabled: configurable && !embeddedDetails, retry: false, staleTime: 0, gcTime: 0,
    });
    const data = embeddedDetails ?? details.data;
    const steps = useMemo(() => {
        const result: WizardStep[] = [];
        const optional = data?.hasOptionalExtras ? data.optionalExtras : [];
        const boosts = data?.hasBoosts ? data.boosts : [];
        if (optional.length) result.push({ id: "optional", title: "Opcionais gratuitos", min: 0, max: optional.length,
            options: [...optional].sort((a, b) => a.displayOrder - b.displayOrder).map(option => ({
                id: option.id, name: option.optionalExtraName, price: 0,
            })) });
        if (boosts.length) result.push({ id: "boosts", title: "Adicionais pagos", min: 0, max: boosts.length,
            options: [...boosts].sort((a, b) => a.displayOrder - b.displayOrder).map(option => ({
                id: option.id, name: option.boostName, price: option.incrementalValue,
            })) });
        return [...result, ...complementSteps(product.complementGroups)];
    }, [data, product.complementGroups]);
    return <ProductWizard name={product.name} description={product.description} imageUrl={product.imageUrl}
        steps={steps} quantity={quantity} basePrice={Math.round((data?.salePrice ?? product.salePrice) * (1 - discountRate) * 100) / 100}
        loading={configurable && !embeddedDetails && details.isPending} loadError={configurable && !embeddedDetails && details.isError}
        onRetry={() => void details.refetch()} submitting={submitting} error={error} onCancel={onCancel}
        onConfirm={selected => onConfirm(
            steps.flatMap(step => step.groupId === undefined ? [] : (selected[step.id] ?? []).map(id => ({
                complementGroupId: step.groupId!, complementId: id,
            }))), selected.optional ?? [], selected.boosts ?? [])} />;
}
