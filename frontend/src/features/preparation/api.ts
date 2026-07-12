import { api } from "../../lib/apiClient";
import type { PreparationTicketResponse } from "../../lib/types";

export const getPreparationQueue = (branchId: number): Promise<PreparationTicketResponse[]> =>
  api<PreparationTicketResponse[]>(`/api/preparation/queue/branch/${branchId}`);

export const advanceItemStatus = (
  orderId: number,
  itemId: number,
  orderItemStatusId: number,
  actorEmployeeId: number | null = null,
): Promise<void> =>
  api<void>(`/api/preparation/orders/${orderId}/items/${itemId}/status`, {
    method: "PUT",
    body: JSON.stringify({ orderItemStatusId, actorEmployeeId }),
  });
