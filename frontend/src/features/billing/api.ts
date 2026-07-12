import { api } from "../../lib/apiClient";
import type { SessionSaleResponse } from "../../lib/types";

export interface SalePaymentInput {
  paymentMethodId: number;
  amount: number;
  changeAmount: number | null;
  authorizationCode: string | null;
}

export const registerPartialPayment = (payload: {
  customerOrderId: number;
  cashSessionId: number;
  employeeId: number;
  paymentMethodId: number;
  amount: number;
  authorizationCode: string | null;
  payerName: string | null;
}): Promise<number> =>
  api<number>("/api/sales/partial", { method: "POST", body: JSON.stringify(payload) });

export const registerSale = (
  customerOrderId: number,
  cashSessionId: number,
  employeeId: number,
  payments: SalePaymentInput[],
): Promise<number> =>
  api<number>("/api/sales", {
    method: "POST",
    body: JSON.stringify({ customerOrderId, cashSessionId, employeeId, payments }),
  });

export const getSalesBySession = (sessionId: number): Promise<SessionSaleResponse[]> =>
  api<SessionSaleResponse[]>(`/api/sales/session/${sessionId}`);

export const refundSale = (saleId: number, employeeId: number, reason: string | null): Promise<void> =>
  api<void>(`/api/sales/${saleId}/refund`, {
    method: "PUT",
    body: JSON.stringify({ employeeId, reason }),
  });
