import { api } from "../../lib/apiClient";
import type { RoleResponse, UserResponse } from "../../lib/types";

export const getUsersByCompany = (companyId: number): Promise<UserResponse[]> =>
  api<UserResponse[]>(`/api/users/company/${companyId}`);

export const getRoles = (companyId: number): Promise<RoleResponse[]> =>
  api<RoleResponse[]>(`/api/users/roles/company/${companyId}`);

export const createUser = (payload: {
  companyId: number;
  employeeId: number | null;
  userName: string;
  email: string;
  password: string;
  roleIds: number[];
}): Promise<number> =>
  api<number>("/api/users", { method: "POST", body: JSON.stringify(payload) });

export const updateUserRoles = (id: number, roleIds: number[]): Promise<void> =>
  api<void>(`/api/users/${id}/roles`, { method: "PUT", body: JSON.stringify({ roleIds }) });

export const deactivateUser = (id: number): Promise<void> =>
  api<void>(`/api/users/${id}/deactivate`, { method: "PUT" });
