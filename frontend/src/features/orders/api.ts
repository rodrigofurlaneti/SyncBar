import { api } from "../../lib/apiClient";
import type { OrderResponse } from "../../lib/types";

export interface OpenOrderPayload {
  branchId: number;
  diningTableId: number | null;
  comandaId: number | null;
  employeeId: number;
  guestCount: number | null;
  notes: string | null;
}

export const getOrder = (id: number): Promise<OrderResponse> =>
  api<OrderResponse>(`/api/orders/${id}`);

export const getOpenOrdersByBranch = (branchId: number): Promise<OrderResponse[]> =>
  api<OrderResponse[]>(`/api/orders/open/branch/${branchId}`);

export const openOrder = (payload: OpenOrderPayload): Promise<number> =>
  api<number>("/api/orders", { method: "POST", body: JSON.stringify(payload) });

export const addOrderItem = (
  orderId: number,
  productId: number,
  quantity: number,
  notes: string | null,
  employeeId: number | null,
): Promise<void> =>
  api<void>(`/api/orders/${orderId}/items`, {
    method: "POST",
    body: JSON.stringify({ productId, quantity, notes, employeeId }),
  });

export const updateItemStatus = (
  orderId: number,
  itemId: number,
  orderItemStatusId: number,
): Promise<void> =>
  api<void>(`/api/orders/${orderId}/items/${itemId}/status`, {
    method: "PUT",
    body: JSON.stringify({ orderItemStatusId }),
  });

export const applyDiscount = (orderId: number, discountAmount: number): Promise<void> =>
  api<void>(`/api/orders/${orderId}/discount`, {
    method: "PUT",
    body: JSON.stringify({ discountAmount }),
  });

export const closeOrder = (orderId: number, serviceFeeRate = 0.1): Promise<void> =>
  api<void>(`/api/orders/${orderId}/close`, {
    method: "PUT",
    body: JSON.stringify({ serviceFeeRate }),
  });

export const raiseCreditLimit = (orderId: number, newLimitAmount: number): Promise<void> =>
  api<void>(`/api/orders/${orderId}/credit-limit`, {
    method: "PUT",
    body: JSON.stringify({ newLimitAmount }),
  });

export const removeServiceFee = (orderId: number): Promise<void> =>
  api<void>(`/api/orders/${orderId}/remove-service-fee`, { method: "PUT" });

export const cancelOrder = (orderId: number): Promise<void> =>
  api<void>(`/api/orders/${orderId}/cancel`, { method: "PUT" });
