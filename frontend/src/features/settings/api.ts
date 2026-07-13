import { api } from "../../lib/apiClient";
import type { ServiceFeeSettingResponse } from "../../lib/types";

export const getServiceFeeSetting = (branchId: number): Promise<ServiceFeeSettingResponse> =>
  api<ServiceFeeSettingResponse>(`/api/orders/service-fee-setting/branch/${branchId}`);

export const setServiceFeeEnabled = (branchId: number, enabled: boolean): Promise<void> =>
  api<void>("/api/orders/service-fee-setting", {
    method: "PUT",
    body: JSON.stringify({ branchId, enabled }),
  });
