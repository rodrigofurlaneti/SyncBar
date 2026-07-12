import { api } from "../../lib/apiClient";
import type { ComandaResponse, ComandaSettingResponse } from "../../lib/types";

export const getComandasByBranch = (branchId: number): Promise<ComandaResponse[]> =>
  api<ComandaResponse[]>(`/api/comandas/branch/${branchId}`);

export const getComandaSetting = (branchId: number): Promise<ComandaSettingResponse> =>
  api<ComandaSettingResponse>(`/api/comandas/settings/branch/${branchId}`);

export const setComandaDefaultLimit = (branchId: number, defaultLimitAmount: number): Promise<void> =>
  api<void>("/api/comandas/settings", {
    method: "PUT",
    body: JSON.stringify({ branchId, defaultLimitAmount }),
  });
