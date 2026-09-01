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
    // Quando informado, o pedido vai pra conta da COMANDA (não da mesa) — a mesa
    // continua registrada no pedido pra cozinha/garçom saberem onde entregar.
    comandaCode?: string,
): Promise<{ orderId: number }> =>
    publicApi<{ orderId: number }>(`/api/publicordering/${token}/items`, {
        method: "POST",
        body: JSON.stringify({ productId, quantity, notes, complements: complements ?? null, comandaCode: comandaCode || null }),
    });

export const getPublicBill = (token: string): Promise<PublicBillResponse> =>
    publicApi<PublicBillResponse>(`/api/publicordering/${token}/bill`);

export const getPublicComandaBill = (token: string, comandaCode: string): Promise<PublicComandaBillResponse> =>
    publicApi<PublicComandaBillResponse>(`/api/publicordering/${token}/comandas/${comandaCode}/bill`);

// Comprovação de leitura da comanda (câmera/código de barras/QR Code) — exigida antes de
// consultar ou abrir pedido numa comanda quando a filial liga algum desses cenários
// (ver DiningTable.IsCameraInputEnabled/IsBarcodeEnabled/IsQrCodeEnabled, refletidos em
// PublicMenuResponse). Basta completar UM dos métodos ligados.
export const validateComandaReading = (
    token: string,
    comandaCode: string,
    payload: { method: "camera" | "barcode" | "qrcode"; scannedValue?: string; photoBase64?: string },
): Promise<void> =>
    publicApi<void>(`/api/publicordering/${token}/comandas/${comandaCode}/reading-validation`, {
        method: "POST",
        body: JSON.stringify(payload),
    });

// Irmã da validação de comanda acima, mas pra MESA — usada quando a "Visualização do
// Cliente (QR Code)" está desligada (sem fluxo de comanda pro cliente) e mesmo assim
// câmera/código de barras/QR Code estão ligados: precisa completar um deles antes de
// liberar qualquer pedido direto na mesa. Sem código de comanda — a mesa já é
// identificada pelo próprio token.
export const validateTableReading = (
    token: string,
    payload: { method: "camera" | "barcode" | "qrcode"; scannedValue?: string; photoBase64?: string },
): Promise<void> =>
    publicApi<void>(`/api/publicordering/${token}/reading-validation`, {
        method: "POST",
        body: JSON.stringify(payload),
    });
