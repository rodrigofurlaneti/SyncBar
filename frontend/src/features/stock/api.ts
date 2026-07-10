import { api } from "../../lib/apiClient";
import type { StockItemResponse, StockMovementResponse } from "../../lib/types";

export const getStockByBranch = (branchId: number): Promise<StockItemResponse[]> =>
  api<StockItemResponse[]>(`/api/stock/branch/${branchId}`);

export const getStockLedger = (stockItemId: number): Promise<StockMovementResponse[]> =>
  api<StockMovementResponse[]>(`/api/stock/${stockItemId}/movements`);

export const registerStockMovement = (payload: {
  branchId: number;
  productId: number;
  stockMovementTypeId: number;
  employeeId: number;
  quantity: number;
  unitCost: number | null;
  documentNumber: string | null;
  notes: string | null;
}): Promise<number> =>
  api<number>("/api/stock/movements", { method: "POST", body: JSON.stringify(payload) });

export const setStockLimits = (
  id: number,
  minimumQuantity: number,
  maximumQuantity: number | null,
): Promise<void> =>
  api<void>(`/api/stock/${id}/limits`, {
    method: "PUT",
    body: JSON.stringify({ minimumQuantity, maximumQuantity }),
  });

export interface InventoryAdjustment {
  productId: number;
  previousQuantity: number;
  countedQuantity: number;
  difference: number;
}

export const adjustInventory = (
  branchId: number,
  employeeId: number,
  counts: { productId: number; countedQuantity: number }[],
): Promise<InventoryAdjustment[]> =>
  api<InventoryAdjustment[]>("/api/stock/inventory", {
    method: "POST",
    body: JSON.stringify({ branchId, employeeId, counts }),
  });
