import { api } from "../../lib/apiClient";
import type { PurchaseResponse, SupplierResponse } from "../../lib/types";

export const getSuppliersByCompany = (companyId: number): Promise<SupplierResponse[]> =>
  api<SupplierResponse[]>(`/api/suppliers/company/${companyId}`);

export interface SupplierPayload {
  companyId: number;
  legalName: string;
  tradeName: string | null;
  cnpj: string | null;
  email: string | null;
  phone: string | null;
}

export const createSupplier = (payload: SupplierPayload): Promise<number> =>
  api<number>("/api/suppliers", { method: "POST", body: JSON.stringify(payload) });

export const deactivateSupplier = (id: number): Promise<void> =>
  api<void>(`/api/suppliers/${id}/deactivate`, { method: "PUT" });

export const getPurchasesByBranch = (branchId: number): Promise<PurchaseResponse[]> =>
  api<PurchaseResponse[]>(`/api/purchases/branch/${branchId}`);

export interface PurchaseItemPayload {
  productId: number;
  quantity: number;
  unitCost: number;
}

export interface PurchasePayload {
  branchId: number;
  supplierId: number;
  employeeId: number;
  documentNumber: string | null;
  purchasedAt: string;
  notes: string | null;
  items: PurchaseItemPayload[];
}

export const registerPurchase = (payload: PurchasePayload): Promise<number> =>
  api<number>("/api/purchases", { method: "POST", body: JSON.stringify(payload) });
