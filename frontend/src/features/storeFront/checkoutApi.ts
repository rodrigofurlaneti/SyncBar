import { api } from "../../lib/apiClient";
import { resolvePaymentMethodSetting, type BranchPaymentMethodSettingResponse } from "../asaas/api";

export const getStorefrontPaymentMethods = async (branchId: number, companyId?: number): Promise<BranchPaymentMethodSettingResponse | null> => {
    const setting = await api<BranchPaymentMethodSettingResponse | null>(`/api/branch-payment-method-settings/branch/${branchId}`);
    if (setting) return setting;
    if (!companyId) throw new Error("Empresa da filial indisponível. Atualize o cardápio e tente novamente.");
    return resolvePaymentMethodSetting(companyId, branchId);
};

export type NewCardPayload = {
    holderName: string;
    number: string;
    expiryMonth: string;
    expiryYear: string;
    ccv: string;
};

export type PixPaymentResult = {
    paymentId: number;
    asaasPaymentId: string;
    status: string;
    pixQrCodeBase64: string | null;
    pixPayload: string | null;
    invoiceUrl: string | null;
    value: number;
};

export type CreditCardPaymentResult = {
    paymentId: number;
    asaasPaymentId: string;
    status: string;
    cardBrand: string | null;
    last4Digits: string | null;
    value: number;
};

export type BoletoPaymentResult = {
    paymentId: number;
    asaasPaymentId: string;
    status: string;
    bankSlipUrl: string | null;
    identificationField: string | null;
    barCode: string | null;
    value: number;
    dueDate: string;
};

export type AsaasPaymentStatus = {
    id: number;
    status: string;
};

export const payWithPix = (customerOrderId: number): Promise<PixPaymentResult> =>
    api<PixPaymentResult>(`/api/checkout/pix`, {
        method: "POST",
        body: JSON.stringify({ customerOrderId }),
    });

export const payWithCreditCard = (payload: {
    customerOrderId: number;
    savedCardId?: number | null;
    card?: NewCardPayload | null;
    saveCard?: boolean;
}): Promise<CreditCardPaymentResult> =>
    api<CreditCardPaymentResult>(`/api/checkout/cartao`, {
        method: "POST",
        body: JSON.stringify(payload),
    });

export const payWithBoleto = (customerOrderId: number): Promise<BoletoPaymentResult> =>
    api<BoletoPaymentResult>(`/api/checkout/boleto`, {
        method: "POST",
        body: JSON.stringify({ customerOrderId }),
    });

// Endpoint já existente (AsaasPaymentController) — usado para o polling de confirmação.
export const getPaymentStatusByOrder = (customerOrderId: number): Promise<AsaasPaymentStatus> =>
    api<AsaasPaymentStatus>(`/api/asaas/payments/order/${customerOrderId}`, {
        method: "GET",
    });
