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
  // Bruto do iFood — "IFOOD" = logística do próprio iFood; qualquer outro valor (ex.:
  // "MERCHANT") = self-delivery/frota própria, elegível pro fluxo de Logística (fase 7). Nulo
  // pra TAKEOUT/DINE_IN ou quando o iFood não informou o campo.
  deliveredBy: string | null;
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

// Fase 9b — rastreamento (posição do entregador) e código de retirada do módulo Order (pedidos
// que vieram do iFood), mais disputas Handshake (aceitar/rejeitar por id informado manualmente —
// ver comentário no backend, AcceptIFoodDisputeCommand).
export interface IFoodOrderTrackingResponse {
  latitude: number | null;
  longitude: number | null;
  expectedDelivery: string | null;
  deliveryEtaEndMinutes: number | null;
  pickupEtaStartMinutes: number | null;
}

export const getIFoodOrderTracking = (ifoodOrderId: number): Promise<IFoodOrderTrackingResponse> =>
  api<IFoodOrderTrackingResponse>(`/api/integrations/ifood/orders/${ifoodOrderId}/tracking`);

export const validateIFoodPickupCode = (ifoodOrderId: number, code: string): Promise<{ codeMatched: boolean }> =>
  api<{ codeMatched: boolean }>(`/api/integrations/ifood/orders/${ifoodOrderId}/validate-pickup-code`, {
    method: "POST",
    body: JSON.stringify({ code }),
  });

export interface IFoodDisputeActionResponse {
  success: boolean;
  status: string | null;
}

export const acceptIFoodDispute = (branchId: number, disputeId: string): Promise<IFoodDisputeActionResponse> =>
  api<IFoodDisputeActionResponse>(`/api/integrations/ifood/disputes/${encodeURIComponent(disputeId)}/accept`, {
    method: "POST",
    body: JSON.stringify({ branchId }),
  });

export const rejectIFoodDispute = (branchId: number, disputeId: string, reason: string): Promise<IFoodDisputeActionResponse> =>
  api<IFoodDisputeActionResponse>(`/api/integrations/ifood/disputes/${encodeURIComponent(disputeId)}/reject`, {
    method: "POST",
    body: JSON.stringify({ branchId, reason }),
  });

// Fase 9c — fecha os gaps restantes do módulo Order da auditoria de 2026-08-20/21: proposta de
// alternativa em disputa, virtual bag e requestDriver/cancelRequestDriver/verifyDeliveryCode do
// PRÓPRIO módulo Order (distintos dos homônimos em Shipping/Logistics, já cobertos acima).
export const requestIFoodDisputeAlternative = (
  branchId: number,
  disputeId: string,
  alternativeId: string,
  alternativeType: string,
  amount?: number,
  currency?: string,
): Promise<IFoodDisputeActionResponse> =>
  api<IFoodDisputeActionResponse>(
    `/api/integrations/ifood/disputes/${encodeURIComponent(disputeId)}/alternatives/${encodeURIComponent(alternativeId)}`,
    { method: "POST", body: JSON.stringify({ branchId, alternativeType, amount: amount ?? null, currency: currency ?? null }) },
  );

export interface IFoodVirtualBagItem {
  uniqueId: string | null;
  name: string | null;
  quantity: number;
  ean: string | null;
}

export interface IFoodOrderVirtualBagResponse {
  id: string | null;
  shortCode: string | null;
  status: string | null;
  createdAt: string | null;
  merchantName: string | null;
  customerName: string | null;
  items: IFoodVirtualBagItem[];
  grossValueAmount: string | null;
  grossValueCurrency: string | null;
  rawPayload: string | null;
}

export const getIFoodOrderVirtualBag = (ifoodOrderId: number): Promise<IFoodOrderVirtualBagResponse> =>
  api<IFoodOrderVirtualBagResponse>(`/api/integrations/ifood/orders/${ifoodOrderId}/virtual-bag`);

export const requestIFoodOrderDriver = (ifoodOrderId: number): Promise<void> =>
  api<void>(`/api/integrations/ifood/orders/${ifoodOrderId}/request-driver`, { method: "POST" });

export const cancelIFoodOrderDriverRequest = (ifoodOrderId: number): Promise<void> =>
  api<void>(`/api/integrations/ifood/orders/${ifoodOrderId}/cancel-request-driver`, { method: "POST" });

export const verifyIFoodOrderDeliveryCode = (ifoodOrderId: number, code: string): Promise<{ codeMatched: boolean }> =>
  api<{ codeMatched: boolean }>(`/api/integrations/ifood/orders/${ifoodOrderId}/verify-delivery-code`, {
    method: "POST",
    body: JSON.stringify({ code }),
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

// Fase 9c — fecha os gaps restantes do módulo Merchant da auditoria de 2026-08-20/21: listar
// lojas do client_id, ver detalhes de uma loja específica e consultar status por operação (ex.:
// "DELIVERY", "TAKEOUT" — diferente do status geral acima, que só olha a primeira operação).
export interface IFoodMerchantSummaryItem {
  id: string;
  name: string | null;
  corporateName: string | null;
}

export const getIFoodMerchantsList = (companyId: number, page = 1, size = 100): Promise<IFoodMerchantSummaryItem[]> =>
  api<IFoodMerchantSummaryItem[]>(`/api/integrations/ifood/merchant/list/company/${companyId}?page=${page}&size=${size}`);

export interface IFoodMerchantAddress {
  country: string | null;
  state: string | null;
  city: string | null;
  postalCode: string | null;
  district: string | null;
  street: string | null;
  number: string | null;
  latitude: number | null;
  longitude: number | null;
}

export interface IFoodMerchantDetailsResponse {
  id: string | null;
  name: string | null;
  corporateName: string | null;
  description: string | null;
  type: string | null;
  status: string | null;
  createdAt: string | null;
  address: IFoodMerchantAddress | null;
}

export const getIFoodMerchantDetails = (branchId: number): Promise<IFoodMerchantDetailsResponse> =>
  api<IFoodMerchantDetailsResponse>(`/api/integrations/ifood/merchant/details/branch/${branchId}`);

export interface IFoodMerchantStatusByOperationResponse {
  operation: string | null;
  salesChannel: string | null;
  available: boolean;
  state: string | null;
  validations: IFoodMerchantValidationItem[];
}

export const getIFoodMerchantStatusByOperation = (
  branchId: number,
  operation: string,
): Promise<IFoodMerchantStatusByOperationResponse> =>
  api<IFoodMerchantStatusByOperationResponse>(
    `/api/integrations/ifood/merchant/status/branch/${branchId}/operation/${encodeURIComponent(operation)}`,
  );

// Logística por frota própria (fase 7, módulo Logistics) — só se aplica a pedidos DELIVERY com
// deliveredBy diferente de "IFOOD" (ver IFoodOrderResponse.deliveredBy). Tudo sob demanda: cada
// passo é acionado manualmente pela equipe conforme o entregador avança.
export interface IFoodLogisticsDeliveryResponse {
  id: number;
  ifoodOrderId: number;
  ifoodOrderDisplayId: string | null;
  driverName: string;
  driverPhone: string;
  driverVehicleType: string;
  status: string;
  customerName: string | null;
  deliveryAddress: string | null;
  assignedAt: string;
  goingToOriginAt: string | null;
  arrivedAtOriginAt: string | null;
  dispatchedAt: string | null;
  arrivedAtDestinationAt: string | null;
  deliveryCodeVerifiedAt: string | null;
}

export const getIFoodLogisticsDeliveries = (branchId: number): Promise<IFoodLogisticsDeliveryResponse[]> =>
  api<IFoodLogisticsDeliveryResponse[]>(`/api/integrations/ifood/logistics/branch/${branchId}`);

export interface AssignIFoodDriverPayload {
  driverName: string;
  driverPhone: string;
  driverVehicleType: string;
}

export const assignIFoodDriver = (ifoodOrderId: number, payload: AssignIFoodDriverPayload): Promise<void> =>
  api<void>(`/api/integrations/ifood/logistics/order/${ifoodOrderId}/assign-driver`, {
    method: "POST",
    body: JSON.stringify(payload),
  });

export const markIFoodGoingToOrigin = (ifoodOrderId: number): Promise<void> =>
  api<void>(`/api/integrations/ifood/logistics/order/${ifoodOrderId}/going-to-origin`, { method: "POST" });

export const markIFoodArrivedAtOrigin = (ifoodOrderId: number): Promise<void> =>
  api<void>(`/api/integrations/ifood/logistics/order/${ifoodOrderId}/arrived-at-origin`, { method: "POST" });

export const dispatchIFoodLogistics = (ifoodOrderId: number): Promise<void> =>
  api<void>(`/api/integrations/ifood/logistics/order/${ifoodOrderId}/dispatch`, { method: "POST" });

export const markIFoodArrivedAtDestination = (ifoodOrderId: number): Promise<void> =>
  api<void>(`/api/integrations/ifood/logistics/order/${ifoodOrderId}/arrived-at-destination`, { method: "POST" });

export const verifyIFoodDeliveryCode = (
  ifoodOrderId: number,
  code: string,
): Promise<{ codeMatched: boolean }> =>
  api<{ codeMatched: boolean }>(`/api/integrations/ifood/logistics/order/${ifoodOrderId}/verify-delivery-code`, {
    method: "POST",
    body: JSON.stringify({ code }),
  });

// Fase 9c — fecha o gap restante do módulo Logistics da auditoria: detalhes da entrega direto no
// iFood. A doc oficial não documenta o schema de resposta (só "<object>"), então rawPayload é o
// JSON bruto — a tela decide o que exibir dele.
export interface IFoodLogisticsOrderDetailsResponse {
  rawPayload: string | null;
}

export const getIFoodLogisticsOrderDetails = (ifoodOrderId: number): Promise<IFoodLogisticsOrderDetailsResponse> =>
  api<IFoodLogisticsOrderDetailsResponse>(`/api/integrations/ifood/logistics/order/${ifoodOrderId}/details`);

// Shipping (fase 8, módulo Shipping) — entrega, via malha de entregadores do iFood, de pedidos
// que NÃO vieram do iFood (telefone, WhatsApp, balcão). Cotação → pedir motorista → acompanhar →
// cancelar, tudo sob demanda. O iFood não devolve um "status" de entrega neste módulo — Status
// aqui só reflete ações que o SyncBar tomou (DRIVER_REQUESTED/CANCELLED).
export interface IFoodShippingDeliveryResponse {
  id: number;
  orderReference: string | null;
  customerName: string;
  deliveryAddress: string;
  merchantFee: number;
  status: string;
  trackingUrl: string | null;
  requestedAt: string;
  cancelledAt: string | null;
}

export const getIFoodShippingDeliveries = (branchId: number): Promise<IFoodShippingDeliveryResponse[]> =>
  api<IFoodShippingDeliveryResponse[]>(`/api/integrations/ifood/shipping/branch/${branchId}`);

export interface IFoodShippingQuoteResponse {
  quoteId: string;
  grossValue: number;
  discount: number;
  netValue: number;
  deliveryTimeMinMinutes: number;
  deliveryTimeMaxMinutes: number;
  distanceMeters: number;
  expirationAt: string | null;
}

export const getIFoodShippingQuote = (branchId: number, latitude: number, longitude: number): Promise<IFoodShippingQuoteResponse> =>
  api<IFoodShippingQuoteResponse>(
    `/api/integrations/ifood/shipping/branch/${branchId}/quote?latitude=${latitude}&longitude=${longitude}`,
  );

export interface IFoodShippingItemInput {
  name: string;
  externalCode?: string;
  quantity: number;
  unitPrice: number;
}

export interface RequestIFoodShippingDriverPayload {
  branchId: number;
  orderReference?: string;
  customerName: string;
  customerPhoneAreaCode: string;
  customerPhoneNumber: string;
  merchantFee: number;
  quoteId: string;
  postalCode: string;
  streetNumber: string;
  streetName: string;
  complement?: string;
  neighborhood: string;
  city: string;
  state: string;
  country?: string;
  reference?: string;
  latitude?: number;
  longitude?: number;
  items: IFoodShippingItemInput[];
}

export const requestIFoodShippingDriver = (payload: RequestIFoodShippingDriverPayload): Promise<{ id: number }> =>
  api<{ id: number }>("/api/integrations/ifood/shipping", { method: "POST", body: JSON.stringify(payload) });

export interface IFoodShippingTrackingResponse {
  latitude: number | null;
  longitude: number | null;
  expectedDelivery: string | null;
  deliveryEtaEndMinutes: number | null;
  pickupEtaStartMinutes: number | null;
}

export const getIFoodShippingTracking = (id: number): Promise<IFoodShippingTrackingResponse> =>
  api<IFoodShippingTrackingResponse>(`/api/integrations/ifood/shipping/${id}/tracking`);

export interface IFoodShippingCancellationReasonResponse {
  cancelCodeId: string;
  description: string;
}

export const getIFoodShippingCancellationReasons = (id: number): Promise<IFoodShippingCancellationReasonResponse[]> =>
  api<IFoodShippingCancellationReasonResponse[]>(`/api/integrations/ifood/shipping/${id}/cancellation-reasons`);

export const cancelIFoodShippingDelivery = (id: number, reason: string, cancellationCode: number): Promise<void> =>
  api<void>(`/api/integrations/ifood/shipping/${id}/cancel`, {
    method: "POST",
    body: JSON.stringify({ reason, cancellationCode }),
  });

export interface IFoodSafeDeliveryScoreResponse {
  score: string | null;
}

export const getIFoodSafeDeliveryScore = (id: number): Promise<IFoodSafeDeliveryScoreResponse> =>
  api<IFoodSafeDeliveryScoreResponse>(`/api/integrations/ifood/shipping/${id}/safe-delivery-score`);

// Fase 9 — cobertura dos 13 relatórios financeiros restantes (financial/v2.0 ×12 +
// financial/v2.1 ×1) + anticipations/sales (financial/v3.0) via um catálogo genérico. A doc
// oficial não documenta o schema de resposta campo-a-campo pra estes relatórios, então "items"
// é o JSON bruto de cada registro (ver comentário em IIFoodFinancialClient no backend).
export const IFOOD_FINANCIAL_REPORT_TYPES = [
  "SalesAdjustments",
  "Payments",
  "PaymentDetails",
  "Occurrences",
  "MaintenanceFees",
  "IncomeTaxes",
  "Periods",
  "ChargeCancellations",
  "Cancellations",
  "ReceivableRecords",
  "SalesBenefits",
  "AdjustmentsBenefits",
  "SalesV21",
  "AnticipationsV3",
  "SalesV3",
] as const;

export type IFoodFinancialReportType = (typeof IFOOD_FINANCIAL_REPORT_TYPES)[number];

export interface IFoodFinancialReportResponse {
  reportType: string;
  count: number;
  items: string[];
}

export const getIFoodFinancialReport = (
  branchId: number,
  reportType: IFoodFinancialReportType,
  options?: { periodId?: string; rangeStart?: string; rangeEnd?: string },
): Promise<IFoodFinancialReportResponse> => {
  const params = new URLSearchParams();
  if (options?.periodId) params.set("periodId", options.periodId);
  if (options?.rangeStart) params.set("rangeStart", options.rangeStart);
  if (options?.rangeEnd) params.set("rangeEnd", options.rangeEnd);
  const query = params.toString();
  return api<IFoodFinancialReportResponse>(
    `/api/integrations/ifood/financial/branch/${branchId}/reports/${reportType}${query ? `?${query}` : ""}`,
  );
};

export interface IFoodReconciliationOnDemandResponse {
  requestId: string;
  rawPayload: string;
}

// Competence no formato "yyyy-MM".
export const requestIFoodReconciliationOnDemand = (
  branchId: number,
  competence: string,
): Promise<IFoodReconciliationOnDemandResponse> =>
  api<IFoodReconciliationOnDemandResponse>(`/api/integrations/ifood/financial/branch/${branchId}/reconciliation-on-demand`, {
    method: "POST",
    body: JSON.stringify({ competence }),
  });

export interface IFoodReconciliationOnDemandStatusResponse {
  found: boolean;
  rawPayload: string | null;
}

export const getIFoodReconciliationOnDemandStatus = (
  branchId: number,
  requestId: string,
): Promise<IFoodReconciliationOnDemandStatusResponse> =>
  api<IFoodReconciliationOnDemandStatusResponse>(
    `/api/integrations/ifood/financial/branch/${branchId}/reconciliation-on-demand/${encodeURIComponent(requestId)}`,
  );

// Avaliações (fase 9, módulo Review v1.0) — sem persistência local, sempre lido/escrito direto
// no iFood.
export interface IFoodReviewOrderItem {
  createdAt: string | null;
  id: string | null;
  shortId: string | null;
}

export interface IFoodReviewListItem {
  id: string;
  createdAt: string | null;
  discarded: boolean;
  published: boolean;
  comment: string | null;
  moderated: boolean;
  moderationStatus: string | null;
  reply: string | null;
  score: number | null;
  order: IFoodReviewOrderItem | null;
}

export interface IFoodReviewListResponse {
  page: number;
  size: number;
  total: number;
  pageCount: number;
  reviews: IFoodReviewListItem[];
}

export const getIFoodReviews = (
  branchId: number,
  options?: { page?: number; pageSize?: number; dateFrom?: string; dateTo?: string; sort?: string; sortBy?: string },
): Promise<IFoodReviewListResponse> => {
  const params = new URLSearchParams();
  params.set("page", String(options?.page ?? 1));
  params.set("pageSize", String(options?.pageSize ?? 10));
  if (options?.dateFrom) params.set("dateFrom", options.dateFrom);
  if (options?.dateTo) params.set("dateTo", options.dateTo);
  if (options?.sort) params.set("sort", options.sort);
  if (options?.sortBy) params.set("sortBy", options.sortBy);
  return api<IFoodReviewListResponse>(`/api/integrations/ifood/reviews/branch/${branchId}?${params.toString()}`);
};

export interface IFoodReviewAnswerOption {
  id: string;
  title: string | null;
}

export interface IFoodReviewQuestion {
  id: string;
  type: string | null;
  title: string | null;
  answers: IFoodReviewAnswerOption[];
}

export interface IFoodReviewDetailResponse {
  id: string;
  createdAt: string | null;
  discarded: boolean;
  published: boolean;
  comment: string | null;
  customerName: string | null;
  moderated: boolean;
  moderationStatus: string | null;
  reply: string | null;
  score: number | null;
  order: IFoodReviewOrderItem | null;
  questions: IFoodReviewQuestion[];
}

export const getIFoodReviewById = (branchId: number, reviewId: string): Promise<IFoodReviewDetailResponse> =>
  api<IFoodReviewDetailResponse>(`/api/integrations/ifood/reviews/branch/${branchId}/${encodeURIComponent(reviewId)}`);

export interface IFoodReviewReplyResponse {
  createdAt: string | null;
  text: string;
  reviewId: string;
}

export const replyIFoodReview = (branchId: number, reviewId: string, text: string): Promise<IFoodReviewReplyResponse> =>
  api<IFoodReviewReplyResponse>(`/api/integrations/ifood/reviews/branch/${branchId}/${encodeURIComponent(reviewId)}/reply`, {
    method: "POST",
    body: JSON.stringify({ text }),
  });

export interface IFoodReviewSummaryResponse {
  score: number | null;
  totalReviewsCount: number;
  validReviewsCount: number;
}

export const getIFoodReviewsSummary = (branchId: number): Promise<IFoodReviewSummaryResponse> =>
  api<IFoodReviewSummaryResponse>(`/api/integrations/ifood/reviews/branch/${branchId}/summary`);

// Indicadores (fase 9, módulo Analytics v1.0) — 1 endpoint (KPIs de pedidos). "buckets" é o JSON
// bruto de cada grupo agregado (ex.: 1 bucket por canal de venda) — ver ressalva no backend
// (IIFoodAnalyticsClient) sobre o payload padrão usado.
export interface IFoodOrderKpisResponse {
  currentPage: number;
  buckets: string[];
}

export const getIFoodOrderKpis = (
  branchId: number,
  options?: { periodStart?: string; periodEnd?: string; page?: number },
): Promise<IFoodOrderKpisResponse> => {
  const params = new URLSearchParams();
  if (options?.periodStart) params.set("periodStart", options.periodStart);
  if (options?.periodEnd) params.set("periodEnd", options.periodEnd);
  params.set("page", String(options?.page ?? 1));
  return api<IFoodOrderKpisResponse>(`/api/integrations/ifood/analytics/branch/${branchId}/order-kpis?${params.toString()}`);
};
