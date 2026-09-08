import { useQuery } from "@tanstack/react-query";
import { create } from "zustand";
import { persist } from "zustand/middleware";
import { api } from "../../lib/apiClient";
import { useAuthStore } from "../../stores/authStore";

type Register = { id: number; name: string };
const useSelection = create(persist<{ ids: Record<number, number>; select: (branch: number, id: number) => void }>(
    set => ({ ids: {}, select: (branch, id) => set(state => ({ ids: { ...state.ids, [branch]: id } })) }),
    { name: "syncbar-cash-register" },
));

export function useCashRegister(branch?: number) {
    const currentBranch = useAuthStore(state => state.branchId);
    const branchId = branch ?? currentBranch;
    const selection = useSelection();
    const query = useQuery({
        queryKey: ["cash-registers", branchId],
        queryFn: () => api<Register[]>(`/api/cash/registers/branch/${branchId}`),
        enabled: !!branchId,
    });
    const registers = query.data ?? [];
    const selected = registers.find(r => r.id === selection.ids[branchId]) ?? registers[0];
    return { ...query, registers, registerId: selected?.id, registerName: selected?.name,
        selectRegister: (id: number) => selection.select(branchId, id) };
}
