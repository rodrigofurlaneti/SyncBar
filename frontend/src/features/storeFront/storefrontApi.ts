import { api } from "../../lib/apiClient";
import type { MenuItemResponse, OrderItemComplementSelection } from "../../lib/types";

export type StorefrontItemRequest = {
    productId: number;
    quantity: number;
    notes?: string | null;
    complements?: OrderItemComplementSelection[];
};

export type StorefrontMenuResponse = {
    companyId: number;
    items: MenuItemResponse[];
};

export type CustomerAppUserPayload = {
    branchId: number;
    userName: string;
    email: string;
    password: string;
    phone?: string | null;
    cpf: string; 
};

export type CustomerAddressPayload = {
    companyId: number;
    branchId?: number | null;
    customerId?: number | null;
    street: string;
    number: string;
    supplement?: string | null;
    zipCode?: string | null;
};

// Tipo de resposta para o endereço do cliente
export type CustomerAddressResponse = {
    id: number;
    companyId: number;
    branchId?: number | null;
    customerId?: number | null;
    street: string;
    number: string;
    supplement?: string | null;
    zipCode?: string | null;
    lastOrderId?: number | null;
    isActive: boolean;
};

// Payload atualizado do pedido contendo as opções de entrega e novo endereço
export type StorefrontOrderPayload = {
    customerName: string;
    customerPhone?: string | null;
    generalNotes?: string | null;
    items: StorefrontItemRequest[];
    customerId?: number | null;
    deliveryType: "PICKUP" | "DELIVERY";
    addressId?: number | null;
    newAddress?: {
        street: string;
        number: string;
        supplement?: string | null;
        zipCode: string;
    } | null;
};

export type CustomerLoginPayload = {
    email: string;
    password: string;
    companyId: number;
    branchId?: number | null;
};

export type CustomerLoginResponse = {
    accessToken: string;
    expiresAt: string;
    refreshToken: string;
    refreshTokenExpiresAt: string;
    userName: string;
    customerId: number;
    companyId: number;
};

let customerAccessToken: string | null = null;
const customerApi = <T>(path: string, init?: RequestInit) => api<T>(path, {
    ...init, headers: { ...init?.headers, Authorization: customerAccessToken ? `Bearer ${customerAccessToken}` : "" },
}, false);

// Customer authentication must not use or overwrite the administrative session.
export const loginCustomerAppUser = async (payload: CustomerLoginPayload): Promise<CustomerLoginResponse> => {
    customerAccessToken = null;
    const result = await customerApi<CustomerLoginResponse>('/api/auth/customer-login', {
        method: 'POST', body: JSON.stringify(payload),
    });
    customerAccessToken = result.accessToken;
    return result;
};

// Buscar o cardápio público da filial (sem token de mesa)
export const getStorefrontMenu = (branchId: number): Promise<StorefrontMenuResponse> =>
    api<StorefrontMenuResponse>(`/api/storefront/branches/${branchId}/menu`, {
        method: "GET",
    });

// Enviar o pedido em lote do autoatendimento
export const submitStorefrontOrder = (
    branchId: number,
    payload: StorefrontOrderPayload
): Promise<{ orderId: number }> =>
    api<{ orderId: number }>(`/api/storefront/branches/${branchId}/orders`, {
        method: "POST",
        body: JSON.stringify(payload),
    });

// Buscar usuários de clientes da empresa (utilizado para validar login por e-mail)
export const getCustomerAppUsersByCompany = (companyId: number): Promise<any[]> =>
    api<any[]>(`/api/customerappusers/company/${companyId}`, {
        method: "GET",
    });

// Cadastrar um novo cliente e seu acesso web unificado
export const registerCustomerAppUser = (
    payload: CustomerAppUserPayload
): Promise<{ id: number; companyId: number }> =>
    customerApi<{ id: number; companyId: number }>(`/api/storefront/branches/${payload.branchId}/customers`, {
        method: "POST",
        body: JSON.stringify({ userName: payload.userName, email: payload.email,
            password: payload.password, cpf: payload.cpf, phone: payload.phone }),
    });

// Cadastrar o endereço de entrega do novo cliente
export const registerCustomerAddress = (
    payload: CustomerAddressPayload
): Promise<{ id: number }> =>
    customerApi<{ id: number }>(`/api/storefront/customer/addresses`, {
        method: "POST",
        body: JSON.stringify(payload),
    });

// Buscar endereços cadastrados de um cliente específico
export const getCustomerAddressesByCustomer = (customerId: number): Promise<CustomerAddressResponse[]> =>
    customerApi<CustomerAddressResponse[]>(`/api/storefront/customer/addresses/customer/${customerId}`, {
        method: "GET",
    });
