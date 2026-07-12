import { api } from "../../lib/apiClient";

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
