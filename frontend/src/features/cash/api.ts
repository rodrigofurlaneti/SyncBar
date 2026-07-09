import { api } from "../../lib/apiClient";
import type {
  CashSessionResponse,
  CashSummaryResponse,
  CloseCashSessionResponse,
} from "../../lib/types";

export const getOpenSession = (registerId: number): Promise<CashSessionResponse> =>
  api<CashSessionResponse>(`/api/cash/registers/${registerId}/open-session`);

export const getCashSummary = (sessionId: number): Promise<CashSummaryResponse> =>
  api<CashSummaryResponse>(`/api/cash/sessions/${sessionId}/summary`);

export const openCashSession = (
  cashRegisterId: number,
  openedByEmployeeId: number,
  openingAmount: number,
): Promise<number> =>
  api<number>("/api/cash/sessions", {
    method: "POST",
    body: JSON.stringify({ cashRegisterId, openedByEmployeeId, openingAmount }),
  });

export const closeCashSession = (
  sessionId: number,
  closedByEmployeeId: number,
  closingAmount: number,
): Promise<CloseCashSessionResponse> =>
  api<CloseCashSessionResponse>(`/api/cash/sessions/${sessionId}/close`, {
    method: "PUT",
    body: JSON.stringify({ closedByEmployeeId, closingAmount }),
  });

export const registerCashMovement = (
  sessionId: number,
  cashMovementTypeId: number,
  employeeId: number,
  amount: number,
  description: string | null,
): Promise<void> =>
  api<void>(`/api/cash/sessions/${sessionId}/movements`, {
    method: "POST",
    body: JSON.stringify({ cashMovementTypeId, employeeId, amount, description }),
  });
