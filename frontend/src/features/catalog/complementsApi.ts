import { api } from "../../lib/apiClient";
import type {
  ComplementGroupResponse,
  ComplementItemResponse,
  ProductComplementGroupResponse,
} from "../../lib/types";

export const getComplementItems = (companyId: number): Promise<ComplementItemResponse[]> =>
  api<ComplementItemResponse[]>(`/api/complements/items/company/${companyId}`);

export const createComplementItem = (companyId: number, name: string): Promise<number> =>
  api<number>("/api/complements/items", {
    method: "POST",
    body: JSON.stringify({ companyId, name }),
  });

export const updateComplementItem = (id: number, name: string): Promise<void> =>
  api<void>(`/api/complements/items/${id}`, {
    method: "PUT",
    body: JSON.stringify({ name }),
  });

export const deactivateComplementItem = (id: number): Promise<void> =>
  api<void>(`/api/complements/items/${id}/deactivate`, { method: "PUT" });

export const getComplementGroups = (companyId: number): Promise<ComplementGroupResponse[]> =>
  api<ComplementGroupResponse[]>(`/api/complements/groups/company/${companyId}`);

export const createComplementGroup = (
  companyId: number,
  name: string,
  complementGroupTypeId: number,
  minSelection: number,
  maxSelection: number,
): Promise<number> =>
  api<number>("/api/complements/groups", {
    method: "POST",
    body: JSON.stringify({ companyId, name, complementGroupTypeId, minSelection, maxSelection }),
  });

export const updateComplementGroup = (
  id: number,
  name: string,
  complementGroupTypeId: number,
  minSelection: number,
  maxSelection: number,
): Promise<void> =>
  api<void>(`/api/complements/groups/${id}`, {
    method: "PUT",
    body: JSON.stringify({ name, complementGroupTypeId, minSelection, maxSelection }),
  });

export const deactivateComplementGroup = (id: number): Promise<void> =>
  api<void>(`/api/complements/groups/${id}/deactivate`, { method: "PUT" });

export const addComplement = (
  groupId: number,
  complementItemId: number,
  extraPrice: number,
): Promise<number> =>
  api<number>(`/api/complements/groups/${groupId}/complements`, {
    method: "POST",
    body: JSON.stringify({ complementItemId, extraPrice }),
  });

export const updateComplementPrice = (
  groupId: number,
  complementId: number,
  extraPrice: number,
): Promise<void> =>
  api<void>(`/api/complements/groups/${groupId}/complements/${complementId}`, {
    method: "PUT",
    body: JSON.stringify({ extraPrice }),
  });

export const removeComplement = (groupId: number, complementId: number): Promise<void> =>
  api<void>(`/api/complements/groups/${groupId}/complements/${complementId}`, { method: "DELETE" });

export const getProductComplementGroups = (productId: number): Promise<ProductComplementGroupResponse[]> =>
  api<ProductComplementGroupResponse[]>(`/api/complements/products/${productId}`);

export const linkProductComplementGroup = (
  productId: number,
  complementGroupId: number,
  displayOrder: number,
): Promise<number> =>
  api<number>(`/api/complements/products/${productId}/groups`, {
    method: "POST",
    body: JSON.stringify({ complementGroupId, displayOrder }),
  });

export const unlinkProductComplementGroup = (productComplementGroupId: number): Promise<void> =>
  api<void>(`/api/complements/product-groups/${productComplementGroupId}`, { method: "DELETE" });
