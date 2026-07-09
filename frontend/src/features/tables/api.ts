import { api } from "../../lib/apiClient";
import type { TableResponse } from "../../lib/types";

export const getTablesByBranch = (branchId: number): Promise<TableResponse[]> =>
  api<TableResponse[]>(`/api/tables/branch/${branchId}`);
