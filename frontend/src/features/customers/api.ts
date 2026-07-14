import { api } from "../../lib/apiClient";
import type { CustomerResponse } from "../../lib/types";

export const getCustomersByCompany = (companyId: number, search?: string): Promise<CustomerResponse[]> =>
  api<CustomerResponse[]>(
    `/api/customers/company/${companyId}${search && search.trim() !== "" ? `?search=${encodeURIComponent(search.trim())}` : ""}`,
  );

export interface CustomerPayload {
  companyId: number;
  name: string;
  phone: string | null;
  cpf: string | null;
  email: string | null;
}

export const createCustomer = (payload: CustomerPayload): Promise<number> =>
  api<number>("/api/customers", { method: "POST", body: JSON.stringify(payload) });

export const addLoyaltyPoints = (id: number, points: number): Promise<void> =>
  api<void>(`/api/customers/${id}/loyalty-points`, {
    method: "PUT",
    body: JSON.stringify({ points }),
  });
