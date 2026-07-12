import { api, apiUpload } from "../../lib/apiClient";
import type { CategoryResponse, MenuItemResponse } from "../../lib/types";

export const getMenu = (companyId: number): Promise<MenuItemResponse[]> =>
  api<MenuItemResponse[]>(`/api/catalog/menu/company/${companyId}`);

export const getCategories = (companyId: number): Promise<CategoryResponse[]> =>
  api<CategoryResponse[]>(`/api/products/categories/company/${companyId}`);

export const createCategory = (companyId: number, name: string, displayOrder: number): Promise<number> =>
  api<number>("/api/products/categories", {
    method: "POST",
    body: JSON.stringify({ companyId, name, displayOrder }),
  });

export interface ProductPayload {
  categoryId: number;
  unitOfMeasureId: number;
  name: string;
  description: string | null;
  barcode: string | null;
  salePrice: number;
  costPrice: number | null;
  isStockControlled: boolean;
  preparationTimeMinutes: number | null;
}

export const createProduct = (companyId: number, payload: ProductPayload): Promise<number> =>
  api<number>("/api/products", { method: "POST", body: JSON.stringify({ companyId, ...payload }) });

export const updateProduct = (id: number, payload: ProductPayload): Promise<void> =>
  api<void>(`/api/products/${id}`, { method: "PUT", body: JSON.stringify(payload) });

export const deactivateProduct = (id: number): Promise<void> =>
  api<void>(`/api/products/${id}/deactivate`, { method: "PUT" });

export const uploadProductImage = (productId: number, file: File): Promise<{ imageUrl: string }> => {
  const formData = new FormData();
  formData.append("file", file);
  return apiUpload<{ imageUrl: string }>(`/api/products/${productId}/image`, formData);
};
