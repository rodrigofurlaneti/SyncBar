import { api } from "../../lib/apiClient";

// Credenciais do app iFood — por EMPRESA (o app é "centralizado": um client_id/client_secret
// dá acesso a vários merchants). ClientId volta em texto puro (não é segredo); ClientSecret
// nunca volta da API.
export interface IFoodSettingsResponse {
  hasCredentials: boolean;
  clientId: string | null;
  enabled: boolean;
  lastConnectionTestAt: string | null;
  lastConnectionTestSucceeded: boolean | null;
}

export const getIFoodSettings = (companyId: number): Promise<IFoodSettingsResponse> =>
  api<IFoodSettingsResponse>(`/api/integrations/ifood/company/${companyId}`);

export interface SaveIFoodSettingsPayload {
  companyId: number;
  clientId: string;
  clientSecret: string; // vazio = manter o segredo já salvo
  enabled: boolean;
}

export const saveIFoodSettings = (payload: SaveIFoodSettingsPayload): Promise<void> =>
  api<void>("/api/integrations/ifood", { method: "PUT", body: JSON.stringify(payload) });

export interface TestIFoodConnectionResponse {
  success: boolean;
  errorMessage: string | null;
}

export const testIFoodConnection = (companyId: number): Promise<TestIFoodConnectionResponse> =>
  api<TestIFoodConnectionResponse>("/api/integrations/ifood/test-connection", {
    method: "POST",
    body: JSON.stringify({ companyId }),
  });

// Mapeamento loja (filial) → MerchantId do iFood — por filial, diferente das credenciais.
export interface IFoodMerchantMappingResponse {
  branchId: number;
  branchName: string;
  merchantId: string | null;
  merchantUuid: string | null;
}

export const getIFoodMerchantMappings = (companyId: number): Promise<IFoodMerchantMappingResponse[]> =>
  api<IFoodMerchantMappingResponse[]>(`/api/integrations/ifood/merchants/company/${companyId}`);

export interface SetIFoodMerchantMappingPayload {
  branchId: number;
  merchantId: string;
  merchantUuid: string;
}

export const setIFoodMerchantMapping = (payload: SetIFoodMerchantMappingPayload): Promise<void> =>
  api<void>("/api/integrations/ifood/merchants", { method: "PUT", body: JSON.stringify(payload) });

// Pedidos iFood ("fluxo essencial") — a sincronização roda sozinha em segundo plano na API
// (polling a cada 30s); esta tela só acompanha e avança o status manualmente.
export interface IFoodOrderResponse {
  id: number;
  customerOrderId: number;
  ifoodOrderId: string;
  displayId: string | null;
  ifoodOrderType: string;
  status: string;
  confirmDeadlineAt: string;
  confirmedAt: string | null;
  hasUnmappedItems: boolean;
  customerName: string;
  customerPhone: string | null;
  deliveryAddress: string | null;
  totalAmount: number;
  createdAt: string;
}

export const getIFoodOrders = (branchId: number): Promise<IFoodOrderResponse[]> =>
  api<IFoodOrderResponse[]>(`/api/integrations/ifood/orders/branch/${branchId}`);

export const startIFoodOrderPreparation = (ifoodOrderId: number): Promise<void> =>
  api<void>(`/api/integrations/ifood/orders/${ifoodOrderId}/start-preparation`, { method: "POST" });

export const markIFoodOrderReady = (ifoodOrderId: number): Promise<void> =>
  api<void>(`/api/integrations/ifood/orders/${ifoodOrderId}/ready`, { method: "POST" });

export interface IFoodCancellationReasonResponse {
  code: string;
  description: string;
}

export const getIFoodCancellationReasons = (ifoodOrderId: number): Promise<IFoodCancellationReasonResponse[]> =>
  api<IFoodCancellationReasonResponse[]>(`/api/integrations/ifood/orders/${ifoodOrderId}/cancellation-reasons`);

export const cancelIFoodOrder = (ifoodOrderId: number, reasonCode: string): Promise<void> =>
  api<void>(`/api/integrations/ifood/orders/${ifoodOrderId}/cancel`, {
    method: "POST",
    body: JSON.stringify({ reasonCode }),
  });
