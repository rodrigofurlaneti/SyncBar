import { api } from "../../lib/apiClient";
import type { ComandaResponse } from "../../lib/types";

export const getComandasByBranch = (branchId: number): Promise<ComandaResponse[]> =>
  api<ComandaResponse[]>(`/api/comandas/branch/${branchId}`);
