import { api } from "../../lib/apiClient";
import type { TableResponse } from "../../lib/types";

export const getTablesByBranch = (branchId: number): Promise<TableResponse[]> =>
  api<TableResponse[]>(`/api/tables/branch/${branchId}`);

export const generateTableQrToken = (tableId: number): Promise<{ token: string }> =>
  api<{ token: string }>(`/api/tables/${tableId}/qr-token`, { method: "POST" });
