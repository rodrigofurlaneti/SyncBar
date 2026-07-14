import { api } from "../../lib/apiClient";
import type { ReservationResponse } from "../../lib/types";

export const getReservationsByBranch = (
  branchId: number,
  from: string,
  to: string,
): Promise<ReservationResponse[]> =>
  api<ReservationResponse[]>(
    `/api/reservations/branch/${branchId}?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`,
  );

export interface ReservationPayload {
  branchId: number;
  customerName: string;
  customerPhone: string | null;
  partySize: number;
  reservedFor: string;
  notes: string | null;
}

export const createReservation = (payload: ReservationPayload): Promise<number> =>
  api<number>("/api/reservations", { method: "POST", body: JSON.stringify(payload) });

export const confirmReservation = (id: number, diningTableId: number): Promise<void> =>
  api<void>(`/api/reservations/${id}/confirm`, {
    method: "PUT",
    body: JSON.stringify({ diningTableId }),
  });

export const cancelReservation = (id: number): Promise<void> =>
  api<void>(`/api/reservations/${id}/cancel`, { method: "PUT" });
