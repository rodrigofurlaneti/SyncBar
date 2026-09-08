import { useQuery } from "@tanstack/react-query";
import { useAuthStore } from "../../stores/authStore";
import { resolvePaymentMethodSetting } from "../asaas/api";
import { PaymentMethod, paymentMethodLabel } from "../../lib/types";

export function usePaymentMethods(branchId: number) {
    const companyId = useAuthStore(state => state.companyId);
    const query = useQuery({ queryKey: ["branchPaymentMethodSettings", "resolve", companyId, branchId],
        queryFn: () => resolvePaymentMethodSetting(companyId!, branchId), enabled: !!companyId && !!branchId });
    const allowed = (id: number) => {
        if (!query.isSuccess) return false;
        const flags = query.data;
        if (!flags) return true;
        if (id === PaymentMethod.CartaoCredito) return flags.enableCreditCard;
        if (id === PaymentMethod.CartaoDebito) return flags.enableDebitCard;
        if (id === PaymentMethod.Pix) return flags.enablePix;
        return true;
    };
    return { ...query, allowed, methods: Object.entries(paymentMethodLabel).filter(([id]) => allowed(Number(id))) };
}
