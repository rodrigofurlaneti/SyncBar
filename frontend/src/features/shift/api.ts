import { api } from "../../lib/apiClient";
import type { ShiftClosingResponse } from "../../lib/types";

export const getOpenShift = (branchId: number): Promise<ShiftClosingResponse> =>
  api<ShiftClosingResponse>(`/api/shift-closing/open?branchId=${branchId}`);

export const openShift = (branchId: number, openedByEmployeeId: number): Promise<number> =>
  api<number>("/api/shift-closing", {
    method: "POST",
    body: JSON.stringify({ branchId, openedByEmployeeId }),
  });

export const closeShift = (
  shiftClosingId: number,
  closedByEmployeeId: number,
  notes: string | null,
): Promise<ShiftClosingResponse> =>
  api<ShiftClosingResponse>(`/api/shift-closing/${shiftClosingId}/close`, {
    method: "PUT",
    body: JSON.stringify({ closedByEmployeeId, notes }),
  });

export const getShiftHistory = (
  branchId: number,
  referenceYear: number,
  referenceMonth: number,
): Promise<ShiftClosingResponse[]> =>
  api<ShiftClosingResponse[]>(
    `/api/shift-closing/history?branchId=${branchId}&referenceYear=${referenceYear}&referenceMonth=${referenceMonth}`,
  );
