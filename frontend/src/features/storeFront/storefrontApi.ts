import { api } from "../../lib/apiClient";
import type { MenuItemResponse, OrderItemComplementSelection } from "../../lib/types";

export type StorefrontItemRequest = {
    productId: number;
    quantity: number;
    notes?: string | null;
    complements?: OrderItemComplementSelection[];
};

export type StorefrontOrderPayload = {
    customerName: string;
    customerPhone?: string | null;
    generalNotes?: string | null;
    items: StorefrontItemRequest[];
    customerId?: number | null;
};

export type StorefrontMenuResponse = {
    items: MenuItemResponse[];
};

export type CustomerAppUserPayload = {
    companyId: number;
    branchId?: number | null;
    customerId?: number | null;
    userName: string;
    email: string;
    password: string;
    phone?: string | null;
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

// Nova rota conectada ao backend (AuthController)
export const loginCustomerAppUser = (
    payload: CustomerLoginPayload
): Promise<CustomerLoginResponse> =>
    api<CustomerLoginResponse>(`/api/auth/customer-login`, {
        method: "POST",
        body: JSON.stringify(payload),
    });

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
): Promise<{ id: number }> =>
    api<{ id: number }>(`/api/customerappusers`, {
        method: "POST",
        body: JSON.stringify(payload),
    });

// Cadastrar o endereço de entrega do novo cliente
export const registerCustomerAddress = (
    payload: CustomerAddressPayload
): Promise<{ id: number }> =>
    api<{ id: number }>(`/api/customeraddresses`, {
        method: "POST",
        body: JSON.stringify(payload),
    });

// Buscar endereços cadastrados de um cliente específico
export const getCustomerAddressesByCustomer = (customerId: number): Promise<CustomerAddressResponse[]> =>
    api<CustomerAddressResponse[]>(`/api/customeraddresses/customer/${customerId}`, {
        method: "GET",
    });