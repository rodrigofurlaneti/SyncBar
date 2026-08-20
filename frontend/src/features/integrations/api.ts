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
  // Exigido só pelos endpoints de tempo de preparo do módulo Merchant (fase 5) — não é segredo.
  ifoodCustomerId: string | null;
}

export const getIFoodSettings = (companyId: number): Promise<IFoodSettingsResponse> =>
  api<IFoodSettingsResponse>(`/api/integrations/ifood/company/${companyId}`);

export interface SaveIFoodSettingsPayload {
  companyId: number;
  clientId: string;
  clientSecret: string; // vazio = manter o segredo já salvo
  enabled: boolean;
  ifoodCustomerId?: string;
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

// Cardápio iFood ("fluxo essencial") — sincronização roda sozinha (a cada produto/categoria
// criado/editado/desativado); este endpoint é só o botão "Sincronizar agora" da tela.
export interface IFoodCatalogSyncSummary {
  skipped: boolean;
  branchesSynced: number;
  categoriesCreated: number;
  productsSynced: number;
  productsPaused: number;
  errors: number;
}

export const syncIFoodCatalog = (companyId: number): Promise<IFoodCatalogSyncSummary> =>
  api<IFoodCatalogSyncSummary>("/api/integrations/ifood/catalog/sync", {
    method: "POST",
    body: JSON.stringify({ companyId }),
  });

// Financeiro iFood (fase 4) — sincronização roda sozinha 1x/dia; esta tela só lê o resumo do
// período e oferece um botão "Sincronizar agora" pra reenvio manual.
export interface IFoodFinancialEventItem {
  id: number;
  name: string;
  description: string | null;
  amount: number;
  hasTransferImpact: boolean;
  competenceDate: string;
  referenceType: string | null;
  referenceId: string | null;
  linkedIFoodOrderId: number | null;
}

export interface IFoodSettlementItem {
  id: number;
  type: string;
  product: string | null;
  amount: number;
  status: string;
  paymentDate: string | null;
}

export interface IFoodFinancialSummaryResponse {
  periodStart: string;
  periodEnd: string;
  totalFinancialEventsWithTransferImpact: number;
  totalSettlements: number;
  hasDiscrepancy: boolean;
  discrepancyAmount: number;
  events: IFoodFinancialEventItem[];
  settlements: IFoodSettlementItem[];
}

export const getIFoodFinancialSummary = (branchId: number): Promise<IFoodFinancialSummaryResponse> =>
  api<IFoodFinancialSummaryResponse>(`/api/integrations/ifood/financial/branch/${branchId}`);

export const syncIFoodFinancial = (companyId: number): Promise<void> =>
  api<void>("/api/integrations/ifood/financial/sync", {
    method: "POST",
    body: JSON.stringify({ companyId }),
  });

// Operação da loja iFood (fase 5, módulo Merchant) — status, interrupções, horários de
// funcionamento e tempo de preparo. Tudo sob demanda (sem sincronização automática de fundo).
export interface IFoodMerchantValidationItem {
  id: string;
  state: string;
  message: string | null;
}

export interface IFoodMerchantStatusResponse {
  operationState: string | null;
  validations: IFoodMerchantValidationItem[];
}

export const getIFoodMerchantStatus = (branchId: number): Promise<IFoodMerchantStatusResponse> =>
  api<IFoodMerchantStatusResponse>(`/api/integrations/ifood/merchant/status/branch/${branchId}`);

export interface IFoodInterruptionItem {
  id: string;
  description: string | null;
  start: string;
  end: string;
}

export const getIFoodInterruptions = (branchId: number): Promise<IFoodInterruptionItem[]> =>
  api<IFoodInterruptionItem[]>(`/api/integrations/ifood/merchant/interruptions/branch/${branchId}`);

export const createIFoodInterruption = (payload: {
  branchId: number;
  description: string;
  start: string;
  end: string;
}): Promise<void> =>
  api<void>("/api/integrations/ifood/merchant/interruptions", { method: "POST", body: JSON.stringify(payload) });

export const deleteIFoodInterruption = (branchId: number, interruptionId: string): Promise<void> =>
  api<void>(`/api/integrations/ifood/merchant/interruptions/${encodeURIComponent(interruptionId)}?branchId=${branchId}`, {
    method: "DELETE",
  });

export interface IFoodOpeningHourShift {
  dayOfWeek: number; // 0 = domingo .. 6 = sábado
  start: string; // "HH:mm"
  durationMinutes: number;
}

export interface IFoodOpeningHoursResponse {
  shifts: IFoodOpeningHourShift[];
  preparationTimeMinutes: number | null;
  hasIFoodCustomerId: boolean;
}

export const getIFoodOpeningHours = (branchId: number): Promise<IFoodOpeningHoursResponse> =>
  api<IFoodOpeningHoursResponse>(`/api/integrations/ifood/merchant/opening-hours/branch/${branchId}`);

export const saveIFoodOpeningHours = (branchId: number, shifts: IFoodOpeningHourShift[]): Promise<void> =>
  api<void>("/api/integrations/ifood/merchant/opening-hours", {
    method: "PUT",
    body: JSON.stringify({ branchId, shifts }),
  });

export const setIFoodPreparationTime = (branchId: number, minutes: number | null): Promise<void> =>
  api<void>("/api/integrations/ifood/merchant/preparation-time", {
    method: "PUT",
    body: JSON.stringify({ branchId, minutes }),
  });
