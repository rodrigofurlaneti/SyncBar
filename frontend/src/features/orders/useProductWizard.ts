import { useCallback, useState } from "react";

export interface WizardOption { id: number; name: string; price: number }
export interface WizardStep {
    id: string;
    title: string;
    min: number;
    max: number;
    options: WizardOption[];
    groupId?: number;
}

export function useProductWizard(steps: WizardStep[], basePrice: number) {
    const [active, setActive] = useState(0);
    const [selected, setSelected] = useState<Record<string, number[]>>({});
    const step = steps[active];
    const count = (selected[step?.id] ?? []).length;
    const valid = (item: WizardStep) => {
        const count = (selected[item.id] ?? []).length;
        return count >= item.min && count <= item.max;
    };
    const toggle = useCallback((item: WizardStep, id: number) => {
        setSelected(current => {
            const chosen = current[item.id] ?? [];
            if (chosen.includes(id)) return { ...current, [item.id]: chosen.filter(value => value !== id) };
            if (chosen.length >= item.max) return current;
            return { ...current, [item.id]: [...chosen, id] };
        });
    }, []);
    const subtotal = basePrice + steps.reduce((sum, item) => sum + item.options.reduce((total, option) =>
        total + ((selected[item.id] ?? []).includes(option.id) ? option.price : 0), 0), 0);
    const last = active === steps.length - 1;
    return { active, setActive, selected, step, count, toggle, subtotal, last,
        valid: step ? valid(step) : true, allValid: steps.every(valid),
        label: last ? "Adicionar ao pedido" : count === 0 && step?.min === 0 ? "Pular" : "Avançar" };
}

