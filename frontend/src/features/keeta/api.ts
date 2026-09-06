import { api } from "../../lib/apiClient";

// Integração Keeta (Open Delivery) — espelha os controllers do backend:
// KeetaSettingController (credenciais OAuth por empresa/filial), KeetaOAuthController
// (autorização do lojista + refresh de token), KeetaMerchantMappingController (lojas
// autorizadas) e as ações de ciclo de vida do pedido em KeetaOrderController.

// --- Configurações (Setting) --------------------------------------------------------------

export interface KeetaIntegrationSettingResponse {
  id: number;
  companyId: number;
  branchId: number;
  clientId: string | null;
  appId: string | null;
  baseUrl: string;
  hasAccessToken: boolean;
  tokenExpiresAtUtc: string | null;
  updatedAtUtc: string;
}

export const getKeetaSettingByCompanyId = (companyId: number): Promise<KeetaIntegrationSettingResponse> =>
  api<KeetaIntegrationSettingResponse>(`/api/keeta/settings/company/${companyId}`);

export const getKeetaSettingByBranchId = (branchId: number): Promise<KeetaIntegrationSettingResponse> =>
  api<KeetaIntegrationSettingResponse>(`/api/keeta/settings/branch/${branchId}`);

export const resolveKeetaSetting = (companyId: number, branchId?: number | null): Promise<KeetaIntegrationSettingResponse> =>
  api<KeetaIntegrationSettingResponse>(
    `/api/keeta/settings/resolve?companyId=${companyId}${branchId ? `&branchId=${branchId}` : ""}`,
  );

export interface CreateKeetaSettingPayload {
  companyId: number;
  branchId: number;
  clientId: string;
  clientSecret: string;
  appId: string;
  baseUrl?: string | null;
}

export interface CreateKeetaIntegrationSettingResponse {
  id: number;
  companyId: number;
  branchId: number;
  baseUrl: string;
}

export const createKeetaSetting = (payload: CreateKeetaSettingPayload): Promise<CreateKeetaIntegrationSettingResponse> =>
  api<CreateKeetaIntegrationSettingResponse>("/api/keeta/settings", { method: "POST", body: JSON.stringify(payload) });

export interface UpdateKeetaSettingPayload {
  companyId: number;
  clientId?: string | null;
  clientSecret?: string | null;
  appId?: string | null;
  baseUrl?: string | null;
}

export const updateKeetaSetting = (id: number, payload: UpdateKeetaSettingPayload): Promise<void> =>
  api<void>(`/api/keeta/settings/${id}`, { method: "PUT", body: JSON.stringify(payload) });

export const deleteKeetaSetting = (id: number, companyId: number): Promise<void> =>
  api<void>(`/api/keeta/settings/${id}?companyId=${companyId}`, { method: "DELETE" });

// --- OAuth (autorização do lojista) --------------------------------------------------------

export interface RequestKeetaAuthorizationUrlResponse {
  merchantAuthorizationUrl: string;
}

export const getKeetaAuthorizationUrl = (
  companyId: number,
  branchId: number,
  redirectUri: string,
): Promise<RequestKeetaAuthorizationUrlResponse> =>
  api<RequestKeetaAuthorizationUrlResponse>(
    `/api/keeta/oauth/authorization-url?companyId=${companyId}&branchId=${branchId}&redirectUri=${encodeURIComponent(redirectUri)}`,
  );

export interface RefreshKeetaAccessTokenResponse {
  tokenExpiresAtUtc: string | null;
}

export const refreshKeetaAccessToken = (companyId: number, branchId: number): Promise<RefreshKeetaAccessTokenResponse> =>
  api<RefreshKeetaAccessTokenResponse>("/api/keeta/oauth/token/refresh", {
    method: "POST",
    body: JSON.stringify({ companyId, branchId }),
  });

// --- Lojas autorizadas (MerchantMapping) ---------------------------------------------------

export interface KeetaIntegrationMerchantMappingResponse {
  id: number;
  companyId: number;
  branchId: number;
  internalMerchantId: string;
  keetaMerchantId: number;
  storeName: string;
  timeZone: string | null;
  isAuthorized: boolean;
  isOnboarded: boolean;
  lastMenuSyncAtUtc: string | null;
  menuBaseUrl: string | null;
  webhookUrl: string | null;
  createdAtUtc: string;
}

export const getAllKeetaMerchantMappingsByCompany = (companyId: number): Promise<KeetaIntegrationMerchantMappingResponse[]> =>
  api<KeetaIntegrationMerchantMappingResponse[]>(`/api/keeta/merchant-mappings/company/${companyId}`);

// --- Pedidos (Order) ------------------------------------------------------------------------

export interface KeetaIntegrationOrderResponse {
  id: number;
  companyId: number;
  branchId: number;
  customerId: number;
  customerOrderId: number;
  keetaOrderId: string;
  displayId: string;
  internalMerchantId: string;
  keetaMerchantId: number;
  status: string;
  orderType: string;
  deliveredBy: string;
  orderAmount: number;
  currency: string;
  rawOrderJson: string;
  orderCreatedAtUtc: string;
  createdAtUtc: string;
  confirmedAtUtc: string | null;
  readyForPickupAtUtc: string | null;
  concludedAtUtc: string | null;
}

export const getActiveKeetaOrdersByBranch = (branchId: number): Promise<KeetaIntegrationOrderResponse[]> =>
  api<KeetaIntegrationOrderResponse[]>(`/api/keeta/orders/branch/${branchId}/active`);

export const confirmKeetaOrder = (
  orderId: number,
  payload: { reason?: string | null; preparationTimeMinutes?: number | null },
): Promise<void> => api<void>(`/api/keeta/orders/${orderId}/confirm`, { method: "POST", body: JSON.stringify(payload) });

export const markKeetaOrderReadyForPickup = (orderId: number): Promise<void> =>
  api<void>(`/api/keeta/orders/${orderId}/ready-for-pickup`, { method: "POST" });

export const dispatchKeetaOrder = (
  orderId: number,
  payload: { trackingEventType?: string | null; trackingEventMessage?: string | null },
): Promise<void> => api<void>(`/api/keeta/orders/${orderId}/dispatch`, { method: "POST", body: JSON.stringify(payload) });

export const markKeetaOrderDelivered = (orderId: number): Promise<void> =>
  api<void>(`/api/keeta/orders/${orderId}/delivered`, { method: "POST" });

export const requestKeetaOrderCancellation = (
  orderId: number,
  payload: { reason: string; code: string; mode: string },
): Promise<void> =>
  api<void>(`/api/keeta/orders/${orderId}/request-cancellation`, { method: "POST", body: JSON.stringify(payload) });

export const acceptKeetaOrderRefund = (orderId: number): Promise<void> =>
  api<void>(`/api/keeta/orders/${orderId}/accept-refund`, { method: "POST" });

export const rejectKeetaOrderRefund = (orderId: number, payload: { reason: string; code: string }): Promise<void> =>
  api<void>(`/api/keeta/orders/${orderId}/reject-refund`, { method: "POST", body: JSON.stringify(payload) });

export const keetaCancellationReasonLabel: Record<string, string> = {
  SYSTEMIC_ISSUES: "Problemas sistêmicos",
  DUPLICATE_APPLICATION: "Pedido duplicado",
  UNAVAILABLE_ITEM: "Item indisponível",
  RESTAURANT_WITHOUT_DELIVERY_PERSON: "Sem entregador disponível",
  OUTDATED_MENU: "Cardápio desatualizado",
  ORDER_OUTSIDE_THE_DELIVERY_AREA: "Fora da área de entrega",
  BLOCKED_CUSTOMER: "Cliente bloqueado",
  OUTSIDE_DELIVERY_HOURS: "Fora do horário de entrega",
  INTERNAL_DIFFICULTIES_OF_THE_RESTAURANT: "Dificuldades internas do restaurante",
  RISK_AREA: "Área de risco",
  DELIVERY_PROBLEM: "Problema na entrega",
};

export const keetaOrderStatusLabel: Record<string, string> = {
  CREATED: "Criado",
  CONFIRMED: "Confirmado",
  READY_FOR_PICKUP: "Pronto para retirada",
  DISPATCHED: "Despachado",
  DELIVERED: "Entregue",
  CONCLUDED: "Concluído",
  CANCELLATION_REQUESTED: "Cancelamento solicitado",
  CANCELLED: "Cancelado",
  REFUND_ACCEPTED: "Reembolso aceito",
  REFUND_REJECTED: "Reembolso rejeitado",
};
