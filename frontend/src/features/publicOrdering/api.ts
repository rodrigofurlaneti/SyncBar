// Chamadas sem autenticação — o "segredo" é o token do QR Code da mesa.
import type { OrderItemComplementSelection, PublicMenuResponse } from "../../lib/types";

async function publicApi<T>(path: string, init?: RequestInit): Promise<T> {
    const response = await fetch(path, {
        ...init,
        headers: { "Content-Type": "application/json", ...init?.headers },
    });
    if (!response.ok) {
        let detail: string | undefined;
        try {
            const body = (await response.json()) as { detail?: string; title?: string };
            detail = body.detail ?? body.title;
        } catch { /* corpo vazio */ }
        throw new Error(detail ?? "Não foi possível completar a operação.");
    }
    if (response.status === 204) return undefined as T;
    return (await response.json()) as T;
}

export type PublicBillItemResponse = {
    itemId: number;
    productName: string;
    quantity: number;
    unitPrice: number;
    totalPrice: number;
    statusId: number;
    requestedAt: string;
    notes?: string | null;
};

export type PublicBillResponse = {
    orderId: number;
    tableNumber: string;
    status: string;
    subtotalAmount: number;
    discountAmount: number;
    serviceFeeAmount: number;
    totalAmount: number;
    items: PublicBillItemResponse[];
};

export type PublicComandaBillResponse = {
    orderId: number;
    comandaCode: string;
    status: string;
    subtotalAmount: number;
    discountAmount: number;
    serviceFeeAmount: number;
    totalAmount: number;
    creditLimitAmount?: number | null;
    items: PublicBillItemResponse[];
};

export const getPublicMenu = (token: string): Promise<PublicMenuResponse> =>
    publicApi<PublicMenuResponse>(`/api/publicordering/${token}/menu`);

export const addPublicOrderItem = (
    token: string,
    productId: number,
    quantity: number,
    notes: string | null,
    complements?: OrderItemComplementSelection[],
): Promise<{ orderId: number }> =>
    publicApi<{ orderId: number }>(`/api/publicordering/${token}/items`, {
        method: "POST",
        body: JSON.stringify({ productId, quantity, notes, complements: complements ?? null }),
    });

export const getPublicBill = (token: string): Promise<PublicBillResponse> =>
    publicApi<PublicBillResponse>(`/api/publicordering/${token}/bill`);

export const getPublicComandaBill = (token: string, comandaCode: string): Promise<PublicComandaBillResponse> =>
    publicApi<PublicComandaBillResponse>(`/api/publicordering/${token}/comandas/${comandaCode}/bill`);