import { api, apiUpload } from "../../lib/apiClient";
import type { CategoryManagementResponse, CategoryResponse, MenuItemResponse, ProductManagementResponse } from "../../lib/types";

export const getMenu = (companyId: number): Promise<MenuItemResponse[]> =>
  api<MenuItemResponse[]>(`/api/catalog/menu/company/${companyId}`);

export const getCategories = (companyId: number): Promise<CategoryResponse[]> =>
  api<CategoryResponse[]>(`/api/categories/company/${companyId}`);

// Tela de gerenciamento (Cardápio admin, split view) — ao contrário de getMenu/getCategories
// (usados também pelo Cardápio Digital do cliente), estas duas trazem itens desativados,
// com isActive, para alimentar o toggle ativo/inativo e o filtro Ativos/Inativos.
export const getCategoriesForManagement = (companyId: number): Promise<CategoryManagementResponse[]> =>
  api<CategoryManagementResponse[]>(`/api/categories/company/${companyId}/management`);

export const getMenuForManagement = (companyId: number): Promise<ProductManagementResponse[]> =>
  api<ProductManagementResponse[]>(`/api/products/company/${companyId}/management`);

export const createCategory = (companyId: number, name: string, displayOrder: number): Promise<number> =>
  api<number>("/api/categories", {
    method: "POST",
    body: JSON.stringify({ companyId, name, displayOrder }),
  });

export const updateCategory = (id: number, name: string, displayOrder: number): Promise<void> =>
  api<void>(`/api/categories/${id}`, {
    method: "PUT",
    body: JSON.stringify({ name, displayOrder }),
  });

export const deactivateCategory = (id: number): Promise<void> =>
  api<void>(`/api/categories/${id}/deactivate`, { method: "PUT" });

export const activateCategory = (id: number): Promise<void> =>
  api<void>(`/api/categories/${id}/activate`, { method: "PUT" });

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

export const activateProduct = (id: number): Promise<void> =>
  api<void>(`/api/products/${id}/activate`, { method: "PUT" });

export const uploadProductImage = (productId: number, file: File): Promise<{ imageUrl: string }> => {
  const formData = new FormData();
  formData.append("file", file);
  return apiUpload<{ imageUrl: string }>(`/api/products/${productId}/image`, formData);
};
