import { api } from "../../lib/apiClient";

export interface RegisterCompanyInput {
  legalName: string;
  tradeName: string;
  cnpj: string;
  companyEmail?: string;
  companyPhone?: string;
  branchName: string;
  branchCnpj?: string;
  addressStreet?: string;
  addressNumber?: string;
  addressDistrict?: string;
  addressCity?: string;
  addressState?: string;
  addressZipCode?: string;
  adminUserName: string;
  adminEmail: string;
  adminPassword: string;
}

export interface RegisterCompanyResult {
  companyId: number;
  branchId: number;
  adminUserId: number;
}

export const registerCompany = (input: RegisterCompanyInput): Promise<RegisterCompanyResult> =>
  api<RegisterCompanyResult>("/api/companies/register", {
    method: "POST",
    body: JSON.stringify(input),
  });
