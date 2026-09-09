import { useMemo } from "react";
import type { ComplementGroupResponse, OrderItemComplementSelection } from "../../lib/types";
import { ProductWizard } from "./ProductWizard";
import type { WizardStep } from "./useProductWizard";

export function complementSteps(groups: ComplementGroupResponse[]): WizardStep[] {
    return groups.map(group => ({
        id: `group-${group.id}`, title: group.name, min: group.minSelection, max: group.maxSelection,
        groupId: group.id,
        options: group.complements.filter(option => option.isActive).map(option => ({
            id: option.id, name: option.complementItemName, price: option.extraPrice,
        })),
    }));
}

export function ComplementSelectorModal({ productName, groups, onCancel, onConfirm, submitting = false,
    basePrice = 0, error, imageUrl, description }: {
    productName: string;
    groups: ComplementGroupResponse[];
    onCancel: () => void;
    onConfirm: (selections: OrderItemComplementSelection[]) => void;
    confirmLabel?: string;
    submitting?: boolean;
    basePrice?: number;
    error?: string | null;
    imageUrl?: string | null;
    description?: string | null;
}) {
    const steps = useMemo(() => complementSteps(groups), [groups]);
    return <ProductWizard name={productName} description={description} imageUrl={imageUrl} steps={steps}
        basePrice={basePrice} submitting={submitting} error={error} onCancel={onCancel}
        onConfirm={selected => onConfirm(steps.flatMap(step => (selected[step.id] ?? []).map(id => ({
            complementGroupId: step.groupId!, complementId: id,
        }))))} />;
}

