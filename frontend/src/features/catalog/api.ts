import { api } from "../../lib/apiClient";
import type { MenuItemResponse } from "../../lib/types";

export const getMenu = (companyId: number): Promise<MenuItemResponse[]> =>
  api<MenuItemResponse[]>(`/api/catalog/menu/company/${companyId}`);
