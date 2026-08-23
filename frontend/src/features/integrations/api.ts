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
  // Fase 14 — "IMMEDIATE" ou "SCHEDULED"; preparationStartDateTime só vem preenchido quando
  // agendado. Pedidos sincronizados antes da Fase 14 voltam com orderTiming "IMMEDIATE" mesmo
  // que originalmente fossem agendados (dado não existia na tabela antes desta fase).
  orderTiming: "IMMEDIATE" | "SCHEDULED" | string;
  preparationStartDateTime: string | null;
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
  // Fase 13 — antes descartado pelo backend, que só olhava o texto de operationState (vocabulário
  // não documentado pelo iFood). Use este campo pra saber com certeza se a loja está recebendo
  // pedidos; operationState fica só como texto informativo pro operador.
  available: boolean;
  validations: IFoodMerchantValidationItem[];
}

export const getIFoodMerchantStatus = (branchId: number): Promise<IFoodMerchantStatusResponse> =>
  api<IFoodMerchantStatusResponse>(`/api/integrations/ifood/merchant/status/branch/${branchId}`);

// Alertas operacionais do iFood (fase 13) — hoje só populados pelo watcher de status de loja em
// segundo plano (backend verifica a cada 5 minutos e avisa quando uma loja fica indisponível, ou
// volta a ficar disponível, no iFood). Guardados em memória no backend só até serem reconhecidos.
export type IFoodOperationalAlertSeverity = "Info" | "Warning" | "Critical";

export interface IFoodOperationalAlert {
  id: string;
  branchId: number;
  branchName: string;
  title: string;
  message: string;
  severity: IFoodOperationalAlertSeverity;
  createdAtUtc: string;
}

export const getIFoodOperationalAlerts = (companyId: number): Promise<IFoodOperationalAlert[]> =>
  api<IFoodOperationalAlert[]>(`/api/integrations/ifood/alerts/company/${companyId}`);

export const acknowledgeIFoodOperationalAlert = (companyId: number, alertId: string): Promise<void> =>
  api<void>("/api/integrations/ifood/alerts/ack", {
    method: "POST",
    body: JSON.stringify({ companyId, alertId }),
  });

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

// Fase 11 — troca de endereço de entrega em andamento (módulo Shipping, variante "pedido já
// existente no iFood"): o cliente pede pra mudar o endereço pelo app dele durante a corrida; o
// lojista propõe um novo endereço (request) ou aceita/recusa quando é o CLIENTE quem propôs.
export interface IFoodDeliveryAddressChangePayload {
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
}

export const requestIFoodDeliveryAddressChange = (
  ifoodOrderId: number,
  payload: IFoodDeliveryAddressChangePayload,
): Promise<void> =>
  api<void>(`/api/integrations/ifood/shipping/order/${ifoodOrderId}/delivery-address-change`, {
    method: "POST",
    body: JSON.stringify(payload),
  });

export const acceptIFoodDeliveryAddressChange = (ifoodOrderId: number): Promise<void> =>
  api<void>(`/api/integrations/ifood/shipping/order/${ifoodOrderId}/delivery-address-change/accept`, { method: "POST" });

export const denyIFoodDeliveryAddressChange = (ifoodOrderId: number): Promise<void> =>
  api<void>(`/api/integrations/ifood/shipping/order/${ifoodOrderId}/delivery-address-change/deny`, { method: "POST" });

export const confirmIFoodUserAddress = (ifoodOrderId: number): Promise<void> =>
  api<void>(`/api/integrations/ifood/shipping/order/${ifoodOrderId}/user-confirm-address`, { method: "POST" });

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

// Fase 10 — módulo Catalog completo. Tier 1 (v2, viva, já usada pela sincronização automática
// desde a fase 3): CRUD tipado dedicado. Tier 2 (v1, legado): console genérico
// (invokeIFoodCatalogV1Operation) que cobre os 56 endpoints da v1 sem tipagem dedicada — todo
// merchant está em v1 OU v2, nunca nos dois. Ressalva: os nomes de campo abaixo foram confirmados
// contra a collection oficial do Postman, mas os VALORES de exemplo da doc são placeholders
// gerados pelo Postman (schema mock), não tráfego real capturado — estrutura confirmada, valores
// não confirmados até testar contra o sandbox.

// --- Categories --------------------------------------------------------------------------------

export interface IFoodCatalogSummaryResponse {
  catalogId: string | null;
  status: string | null;
  context: string[] | null;
  groupId: string | null;
  modifiedAt: string | null;
}

export const getIFoodCatalogs = (branchId: number): Promise<IFoodCatalogSummaryResponse[]> =>
  api<IFoodCatalogSummaryResponse[]>(`/api/integrations/ifood/catalog/branch/${branchId}/catalogs`);

export interface IFoodCategoryResponse {
  id: string | null;
  index: number | null;
  name: string | null;
  externalCode: string | null;
  status: string | null;
  template: string | null;
}

export const listIFoodCategories = (branchId: number, catalogId: string, includeItems = false): Promise<IFoodCategoryResponse[]> =>
  api<IFoodCategoryResponse[]>(
    `/api/integrations/ifood/catalog/branch/${branchId}/catalogs/${encodeURIComponent(catalogId)}/categories?includeItems=${includeItems}`,
  );

export const getIFoodCategory = (
  branchId: number,
  catalogId: string,
  categoryId: string,
  includeItems = false,
): Promise<IFoodCategoryResponse> =>
  api<IFoodCategoryResponse>(
    `/api/integrations/ifood/catalog/branch/${branchId}/catalogs/${encodeURIComponent(catalogId)}/categories/${encodeURIComponent(categoryId)}?includeItems=${includeItems}`,
  );

export const createIFoodCategory = (branchId: number, catalogId: string, name: string): Promise<{ ifoodCategoryId: string | null }> =>
  api<{ ifoodCategoryId: string | null }>(
    `/api/integrations/ifood/catalog/branch/${branchId}/catalogs/${encodeURIComponent(catalogId)}/categories`,
    { method: "POST", body: JSON.stringify({ name }) },
  );

export const editIFoodCategory = (
  branchId: number,
  catalogId: string,
  categoryId: string,
  payload: { name?: string; externalCode?: string; status?: string; index?: number },
): Promise<IFoodCategoryResponse> =>
  api<IFoodCategoryResponse>(
    `/api/integrations/ifood/catalog/branch/${branchId}/catalogs/${encodeURIComponent(catalogId)}/categories/${encodeURIComponent(categoryId)}`,
    { method: "PUT", body: JSON.stringify(payload) },
  );

export const deleteIFoodCategory = (branchId: number, categoryId: string): Promise<void> =>
  api<void>(`/api/integrations/ifood/catalog/branch/${branchId}/categories/${encodeURIComponent(categoryId)}`, { method: "DELETE" });

export interface IFoodSellableItemResponse {
  itemId: string | null;
  categoryId: string | null;
  itemName: string | null;
  itemExternalCode: string | null;
  itemEan: string | null;
  itemPriceValue: number | null;
}

export const listIFoodSellableItems = (branchId: number, groupId: string): Promise<IFoodSellableItemResponse[]> =>
  api<IFoodSellableItemResponse[]>(
    `/api/integrations/ifood/catalog/branch/${branchId}/sellable-items?groupId=${encodeURIComponent(groupId)}`,
  );

// --- Products ------------------------------------------------------------------------------------

export interface IFoodProductResponse {
  id: string | null;
  name: string | null;
  description: string | null;
  additionalInformation: string | null;
  externalCode: string | null;
  ean: string | null;
  industrialized: boolean | null;
  imagePath: string | null;
}

export const listIFoodProducts = (branchId: number, limit?: number, page?: number): Promise<IFoodProductResponse[]> => {
  const params = new URLSearchParams();
  if (limit != null) params.set("limit", String(limit));
  if (page != null) params.set("page", String(page));
  const query = params.toString();
  return api<IFoodProductResponse[]>(`/api/integrations/ifood/catalog/branch/${branchId}/products${query ? `?${query}` : ""}`);
};

export interface IFoodProductShiftInput {
  startTime: string;
  endTime: string;
  monday: boolean;
  tuesday: boolean;
  wednesday: boolean;
  thursday: boolean;
  friday: boolean;
  saturday: boolean;
  sunday: boolean;
}

export interface CreateIFoodProductPayload {
  id?: string;
  name: string;
  description?: string;
  additionalInformation?: string;
  externalCode?: string;
  ean?: string;
  image?: string;
  shifts?: IFoodProductShiftInput[];
}

export const createIFoodProduct = (branchId: number, payload: CreateIFoodProductPayload): Promise<IFoodProductResponse> =>
  api<IFoodProductResponse>(`/api/integrations/ifood/catalog/branch/${branchId}/products`, {
    method: "POST",
    body: JSON.stringify(payload),
  });

export type EditIFoodProductPayload = Omit<CreateIFoodProductPayload, "id">;

export const editIFoodProduct = (branchId: number, productId: string, payload: EditIFoodProductPayload): Promise<IFoodProductResponse> =>
  api<IFoodProductResponse>(`/api/integrations/ifood/catalog/branch/${branchId}/products/${productId}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });

export const deleteIFoodProduct = (branchId: number, productId: string): Promise<void> =>
  api<void>(`/api/integrations/ifood/catalog/branch/${branchId}/products/${productId}`, { method: "DELETE" });

export interface IFoodBatchProductStatusInput {
  productId?: string;
  externalCode?: string;
  status: string;
  resources?: string[];
}

export const batchUpdateIFoodProductStatuses = (
  branchId: number,
  items: IFoodBatchProductStatusInput[],
  catalogContext?: string,
): Promise<void> =>
  api<void>(`/api/integrations/ifood/catalog/branch/${branchId}/products/status`, {
    method: "PATCH",
    body: JSON.stringify({ items, catalogContext }),
  });

export interface IFoodBatchProductPriceInput {
  productId?: string;
  externalCode?: string;
  value: number;
  originalValue?: number;
  resources?: string[];
}

export const batchUpdateIFoodProductPrices = (
  branchId: number,
  items: IFoodBatchProductPriceInput[],
  catalogContext?: string,
): Promise<{ url: string | null; batchId: string | null }> =>
  api<{ url: string | null; batchId: string | null }>(`/api/integrations/ifood/catalog/branch/${branchId}/products/price`, {
    method: "POST",
    body: JSON.stringify({ items, catalogContext }),
  });

export const listIFoodProductsByExternalCode = (branchId: number, externalCode: string): Promise<IFoodProductResponse[]> =>
  api<IFoodProductResponse[]>(
    `/api/integrations/ifood/catalog/branch/${branchId}/products/externalCode/${encodeURIComponent(externalCode)}`,
  );

export const getIFoodProductById = (branchId: number, productId: string): Promise<IFoodProductResponse> =>
  api<IFoodProductResponse>(`/api/integrations/ifood/catalog/branch/${branchId}/products/${productId}`);

// --- Items (v2 — flat) -----------------------------------------------------------------------------

export interface IFoodItemFlatResponse {
  itemId: string | null;
  status: string | null;
  priceValue: number | null;
  externalCode: string | null;
  categoryId: string | null;
  rawPayload: string | null;
}

export const getIFoodItemFlat = (branchId: number, itemId: string): Promise<IFoodItemFlatResponse> =>
  api<IFoodItemFlatResponse>(`/api/integrations/ifood/catalog/branch/${branchId}/items/${itemId}`);

export interface IFoodItemPriceByCatalogInput {
  value: number;
  catalogContext: string;
  originalValue?: number;
}

export const setIFoodItemPrice = (
  branchId: number,
  itemId: string,
  value: number,
  originalValue?: number,
  priceByCatalog?: IFoodItemPriceByCatalogInput[],
): Promise<void> =>
  api<void>(`/api/integrations/ifood/catalog/branch/${branchId}/items/${itemId}/price`, {
    method: "PUT",
    body: JSON.stringify({ value, originalValue, priceByCatalog }),
  });

export interface IFoodItemExternalCodeByCatalogInput {
  externalCode: string;
  catalogContext: string;
}

export const setIFoodItemExternalCode = (
  branchId: number,
  itemId: string,
  externalCode?: string,
  byCatalog?: IFoodItemExternalCodeByCatalogInput[],
): Promise<void> =>
  api<void>(`/api/integrations/ifood/catalog/branch/${branchId}/items/${itemId}/externalCode`, {
    method: "PUT",
    body: JSON.stringify({ externalCode, byCatalog }),
  });

export const deleteIFoodItem = (branchId: number, categoryId: string, productId: string, catalogContext?: string): Promise<void> =>
  api<void>(
    `/api/integrations/ifood/catalog/branch/${branchId}/categories/${encodeURIComponent(categoryId)}/items/${productId}${
      catalogContext ? `?catalogContext=${encodeURIComponent(catalogContext)}` : ""
    }`,
    { method: "DELETE" },
  );

export const listIFoodCategoryItems = (branchId: number, categoryId: string): Promise<{ rawPayload: string | null }> =>
  api<{ rawPayload: string | null }>(
    `/api/integrations/ifood/catalog/branch/${branchId}/categories/${encodeURIComponent(categoryId)}/items`,
  );

// --- Option groups / options -------------------------------------------------------------------------

export interface IFoodOptionGroupResponse {
  id: string | null;
  name: string | null;
  externalCode: string | null;
  status: string | null;
  index: number | null;
}

export const listIFoodOptionGroups = (
  branchId: number,
  includeOptions = false,
  catalogContext?: string,
): Promise<IFoodOptionGroupResponse[]> => {
  const params = new URLSearchParams();
  params.set("includeOptions", String(includeOptions));
  if (catalogContext) params.set("catalogContext", catalogContext);
  return api<IFoodOptionGroupResponse[]>(`/api/integrations/ifood/catalog/branch/${branchId}/option-groups?${params.toString()}`);
};

export const updateIFoodOptionGroup = (branchId: number, optionGroupId: string, name: string): Promise<void> =>
  api<void>(`/api/integrations/ifood/catalog/branch/${branchId}/option-groups/${optionGroupId}`, {
    method: "PATCH",
    body: JSON.stringify({ name }),
  });

export const deleteIFoodOptionGroup = (branchId: number, optionGroupId: string): Promise<void> =>
  api<void>(`/api/integrations/ifood/catalog/branch/${branchId}/option-groups/${optionGroupId}`, { method: "DELETE" });

export const disassociateIFoodOptionGroup = (branchId: number, optionGroupId: string, productId: string): Promise<void> =>
  api<void>(`/api/integrations/ifood/catalog/branch/${branchId}/option-groups/${optionGroupId}/products/${productId}`, {
    method: "DELETE",
  });

export const deleteIFoodOption = (
  branchId: number,
  optionGroupId: string,
  productId: string,
  catalogContext?: string,
): Promise<void> =>
  api<void>(
    `/api/integrations/ifood/catalog/branch/${branchId}/option-groups/${optionGroupId}/options/${productId}${
      catalogContext ? `?catalogContext=${encodeURIComponent(catalogContext)}` : ""
    }`,
    { method: "DELETE" },
  );

export const updateIFoodOptionGroupStatus = (branchId: number, optionGroupId: string, available: boolean): Promise<void> =>
  api<void>(`/api/integrations/ifood/catalog/branch/${branchId}/option-groups/${optionGroupId}/status`, {
    method: "PATCH",
    body: JSON.stringify({ available }),
  });

export const setIFoodOptionPrice = (
  branchId: number,
  optionId: string,
  value: number,
  originalValue?: number,
  parentCustomizationOptionId?: string,
): Promise<void> =>
  api<void>(`/api/integrations/ifood/catalog/branch/${branchId}/options/${optionId}/price`, {
    method: "PUT",
    body: JSON.stringify({ value, originalValue, parentCustomizationOptionId }),
  });

export const setIFoodOptionExternalCode = (
  branchId: number,
  optionId: string,
  externalCode: string,
  parentCustomizationOptionId?: string,
): Promise<void> =>
  api<void>(`/api/integrations/ifood/catalog/branch/${branchId}/options/${optionId}/externalCode`, {
    method: "PUT",
    body: JSON.stringify({ externalCode, parentCustomizationOptionId }),
  });

export const setIFoodOptionStatus = (
  branchId: number,
  optionId: string,
  available: boolean,
  parentCustomizationOptionId?: string,
): Promise<void> =>
  api<void>(`/api/integrations/ifood/catalog/branch/${branchId}/options/${optionId}/status`, {
    method: "PATCH",
    body: JSON.stringify({ available, parentCustomizationOptionId }),
  });

// --- Admin (estoque, lote, versão do catálogo, imagem) -----------------------------------------------

export interface IFoodInventoryResponse {
  productId: string | null;
  ownerId: string | null;
  amount: number | null;
  inStock: boolean | null;
}

export const getIFoodInventory = (branchId: number, productId: string): Promise<IFoodInventoryResponse> =>
  api<IFoodInventoryResponse>(`/api/integrations/ifood/catalog/branch/${branchId}/inventory/${productId}`);

export const deleteIFoodInventoryBatch = (branchId: number, productIds: string[]): Promise<void> =>
  api<void>(`/api/integrations/ifood/catalog/branch/${branchId}/inventory/batch`, {
    method: "DELETE",
    body: JSON.stringify({ productIds }),
  });

export interface IFoodBatchResultItemResponse {
  resourceId: string | null;
  result: string | null;
  failureReason: string | null;
}

export interface IFoodBatchStatusResponse {
  batchStatus: string | null;
  results: IFoodBatchResultItemResponse[];
}

export const getIFoodBatchResult = (branchId: number, batchId: string): Promise<IFoodBatchStatusResponse> =>
  api<IFoodBatchStatusResponse>(`/api/integrations/ifood/catalog/branch/${branchId}/batch/${encodeURIComponent(batchId)}`);

export const checkIFoodCatalogVersion = (branchId: number): Promise<{ version: string | null }> =>
  api<{ version: string | null }>(`/api/integrations/ifood/catalog/branch/${branchId}/version`);

// ⚠️ Operações destrutivas e irreversíveis no catálogo real do merchant — a UI precisa confirmar
// explicitamente com o usuário antes de chamar.
export const upgradeIFoodCatalogVersion = (branchId: number, cleanMigration?: boolean): Promise<void> =>
  api<void>(`/api/integrations/ifood/catalog/branch/${branchId}/upgrade`, {
    method: "POST",
    body: JSON.stringify({ cleanMigration }),
  });

export const downgradeIFoodCatalogVersion = (branchId: number): Promise<void> =>
  api<void>(`/api/integrations/ifood/catalog/branch/${branchId}/downgrade`, { method: "POST" });

// ⚠️ Schema de corpo/resposta não documentado pelo iFood — repassa o JSON cru fornecido pelo
// chamador; a resposta também é crua (rawPayload). Tratar como não confiável até testar contra o
// sandbox.
export const uploadIFoodImage = (branchId: number, jsonBody: string): Promise<{ rawPayload: string | null }> =>
  api<{ rawPayload: string | null }>(`/api/integrations/ifood/catalog/branch/${branchId}/image`, {
    method: "POST",
    body: JSON.stringify({ jsonBody }),
  });

// --- v1 (legado) — console genérico ----------------------------------------------------------------
// Um único endpoint despachante pros 56 endpoints do Catalog v1 sem tipagem dedicada — o chamador
// escolhe a operação e fornece os parâmetros de rota/query/corpo que ela precisa. A resposta
// (inclusive erro do iFood) é sempre repassada, mesmo em falha HTTP — ver comentário no backend
// (InvokeIFoodCatalogV1OperationCommandHandler).
export const IFOOD_CATALOG_V1_OPERATIONS = [
  "ListCatalogs", "ListUnsellableItems", "ListCategories", "CreateCategory", "GetCategory", "EditCategory",
  "DeleteCategory", "ListSellableItems", "EditAisleGroupId", "UpdateItemStatusByItemId",
  "UpdateOptionStatusByItemIdAndOptionId", "GetItem", "EditItemStatus", "CreateItem", "EditItem", "DeleteItem",
  "CreateOptionGroup", "ListOptionGroups", "UpdateOptionGroup", "DeleteOptionGroup", "AssociateOptionGroupToProduct",
  "UpdateOptionGroupProductAssociation", "DisassociateOptionGroupFromProduct", "CreateOption", "UpdateOption",
  "DeleteOption", "UpdateOptionGroupStatus", "ListProducts", "CreateProduct", "EditProduct", "DeleteProduct",
  "UpdateProductStatus", "BatchUpdateProductStatuses", "BatchUpdateProductPrices", "ListProductsByExternalCode",
  "BatchUpdateStatusByExternalCode", "GetProductById", "CreatePizza", "ListPizzas", "UpdatePizza",
  "UpdatePizzaStatus", "LinkPizzaToCategory", "UnlinkPizzaFromCategory", "BatchUpdatePizzaPricesByExternalCode",
  "BatchUpdatePizzaPrices", "GetBatchResults", "UpsertInventory", "GetInventory", "DeleteInventoryBatch",
  "MultisetupUpsertItem", "MultisetupUpdateOptionPrice", "MultisetupUpdateOptionStatus", "MultisetupDeleteCategory",
  "MultisetupListCategoryItems", "MultisetupDeleteOptionGroup", "MultisetupIsMultisetup",
] as const;

export type IFoodCatalogV1Operation = (typeof IFOOD_CATALOG_V1_OPERATIONS)[number];

export interface IFoodCatalogV1OperationResponse {
  success: boolean;
  statusCode: number;
  responseBody: string | null;
  errorMessage: string | null;
}

// A API não registra um JsonStringEnumConverter (ver Program.cs) — o enum IFoodCatalogV1Operation
// trafega pelo corpo JSON como o inteiro do seu valor ordinal (índice na declaração do enum no
// backend), não como string. IFOOD_CATALOG_V1_OPERATIONS está na MESMA ORDEM da declaração C#
// (ver IIFoodCatalogClient.cs) — o índice do nome no array É o ordinal a enviar.
export const invokeIFoodCatalogV1Operation = (
  branchId: number,
  operation: IFoodCatalogV1Operation,
  options?: { routeParams?: Record<string, string>; queryParams?: Record<string, string>; jsonBody?: string },
): Promise<IFoodCatalogV1OperationResponse> =>
  api<IFoodCatalogV1OperationResponse>(`/api/integrations/ifood/catalog/branch/${branchId}/v1/invoke`, {
    method: "POST",
    body: JSON.stringify({
      operation: IFOOD_CATALOG_V1_OPERATIONS.indexOf(operation),
      routeParams: options?.routeParams ?? null,
      queryParams: options?.queryParams ?? null,
      jsonBody: options?.jsonBody ?? null,
    }),
  });
