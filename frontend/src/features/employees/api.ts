import { api } from "../../lib/apiClient";
import type { EmployeeResponse, JobTitleResponse } from "../../lib/types";

export const getEmployeesByBranch = (branchId: number): Promise<EmployeeResponse[]> =>
  api<EmployeeResponse[]>(`/api/employees/branch/${branchId}`);

export const getJobTitles = (companyId: number): Promise<JobTitleResponse[]> =>
  api<JobTitleResponse[]>(`/api/employees/jobtitles/company/${companyId}`);

export interface EmployeePayload {
  branchId: number;
  jobTitleId: number;
  name: string;
  cpf: string;
  email: string | null;
  phone: string | null;
  hiredAt: string;
  salary: number | null;
}

export const createEmployee = (payload: EmployeePayload): Promise<number> =>
  api<number>("/api/employees", { method: "POST", body: JSON.stringify(payload) });

export const updateEmployee = (
  id: number,
  payload: Pick<EmployeePayload, "jobTitleId" | "name" | "email" | "phone" | "salary">,
): Promise<void> =>
  api<void>(`/api/employees/${id}`, { method: "PUT", body: JSON.stringify(payload) });

export const dismissEmployee = (id: number): Promise<void> =>
  api<void>(`/api/employees/${id}/dismiss`, { method: "PUT" });
