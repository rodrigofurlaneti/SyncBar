import { api } from "../../lib/apiClient";
import type { EmployeeResponse, JobTitleResponse } from "../../lib/types";

export const getEmployeesByBranch = (branchId: number): Promise<EmployeeResponse[]> =>
  api<EmployeeResponse[]>(`/api/employees/branch/${branchId}`);

export const getJobTitles = (companyId: number): Promise<JobTitleResponse[]> =>
  api<JobTitleResponse[]>(`/api/employees/jobtitles/company/${companyId}`);

export const createJobTitle = (companyId: number, name: string): Promise<number> =>
  api<number>("/api/employees/jobtitles", {
    method: "POST",
    body: JSON.stringify({ companyId, name }),
  });

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

// Cadastro único de Equipe (funcionário + usuário do sistema opcional + acessos extras),
// substituindo o fluxo de preencher Nome/E-mail duas vezes em telas separadas e criar um
// "Perfil" manualmente — o perfil de acesso é derivado automaticamente do Cargo no backend.
export interface RegisterTeamMemberPayload extends EmployeePayload {
  hasSystemAccess: boolean;
  userName: string | null;
  userEmail: string | null;
  password: string | null;
  extraFeatureIds: number[] | null;
}

export interface RegisterTeamMemberResult {
  employeeId: number;
  appUserId: number | null;
  accessWarning: string | null;
}

export const registerTeamMember = (
  companyId: number,
  payload: RegisterTeamMemberPayload,
): Promise<RegisterTeamMemberResult> =>
  api<RegisterTeamMemberResult>("/api/employees/team", {
    method: "POST",
    body: JSON.stringify({ companyId, ...payload }),
  });
