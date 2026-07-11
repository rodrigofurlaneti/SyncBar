import { api } from "../../lib/apiClient";
import type { ActivePromotionResponse, PromotionResponse } from "../../lib/types";

export const getPromotionsByBranch = (branchId: number): Promise<PromotionResponse[]> =>
  api<PromotionResponse[]>(`/api/promotions/branch/${branchId}`);

export const getActivePromotions = (branchId: number): Promise<ActivePromotionResponse[]> =>
  api<ActivePromotionResponse[]>(`/api/catalog/promotions/active/branch/${branchId}`);

export const createPromotion = (payload: {
  branchId: number;
  productId: number;
  name: string;
  dayOfWeek: number;
  startMinuteOfDay: number;
  endMinuteOfDay: number;
  promotionTypeId: number;
  discountRate: number | null;
}): Promise<number> =>
  api<number>("/api/promotions", { method: "POST", body: JSON.stringify(payload) });

export const deactivatePromotion = (id: number): Promise<void> =>
  api<void>(`/api/promotions/${id}/deactivate`, { method: "PUT" });
