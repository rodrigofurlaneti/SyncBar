import { api } from "../../lib/apiClient";
import type { ServiceFeeSettingResponse } from "../../lib/types";

export const getServiceFeeSetting = (branchId: number): Promise<ServiceFeeSettingResponse> =>
  api<ServiceFeeSettingResponse>(`/api/orders/service-fee-setting/branch/${branchId}`);

export const setServiceFeeEnabled = (branchId: number, enabled: boolean): Promise<void> =>
  api<void>("/api/orders/service-fee-setting", {
    method: "PUT",
    body: JSON.stringify({ branchId, enabled }),
  });

// Funcionário "dono" dos pedidos abertos pelo autoatendimento via QR Code (obrigatório
// configurar antes de gerar QR Codes de mesa — sem ele o pedido público não sabe a quem atribuir).
export const setSelfServiceEmployee = (branchId: number, employeeId: number | null): Promise<void> =>
  api<void>("/api/branches/self-service-employee", {
    method: "PUT",
    body: JSON.stringify({ branchId, employeeId }),
  });
