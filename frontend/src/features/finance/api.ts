import { api } from "../../lib/apiClient";
import type { BillingSummaryResponse, ScenariosResponse } from "../../lib/types";

export const getBillingSummary = (
  branchId: number,
  year: number,
  month: number,
): Promise<BillingSummaryResponse> =>
  api<BillingSummaryResponse>(`/api/finance/summary/branch/${branchId}/${year}/${month}`);

export const createOperatingCost = (payload: {
  branchId: number;
  costTypeId: number;
  description: string;
  amount: number;
  referenceYear: number;
  referenceMonth: number;
}): Promise<number> =>
  api<number>("/api/finance/costs", { method: "POST", body: JSON.stringify(payload) });

export const deactivateOperatingCost = (id: number): Promise<void> =>
  api<void>(`/api/finance/costs/${id}/deactivate`, { method: "PUT" });

export const setRevenueTarget = (payload: {
  branchId: number;
  referenceYear: number;
  referenceMonth: number;
  targetAmount: number;
}): Promise<number> =>
  api<number>("/api/finance/target", { method: "PUT", body: JSON.stringify(payload) });

export const getScenarios = (
  branchId: number,
  year: number,
  month: number,
  desiredProfit: number,
  margins: { pessimistic?: number; normal?: number; optimistic?: number } = {},
): Promise<ScenariosResponse> => {
  const params = new URLSearchParams({ desiredProfit: String(desiredProfit) });
  if (margins.pessimistic !== undefined) params.set("pessimisticMargin", String(margins.pessimistic));
  if (margins.normal !== undefined) params.set("normalMargin", String(margins.normal));
  if (margins.optimistic !== undefined) params.set("optimisticMargin", String(margins.optimistic));
  return api<ScenariosResponse>(`/api/finance/scenarios/branch/${branchId}/${year}/${month}?${params}`);
};
