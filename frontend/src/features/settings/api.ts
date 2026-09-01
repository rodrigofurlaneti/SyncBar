import { api } from "../../lib/apiClient";
import type { ServiceFeeSettingResponse, TableReadingValidationSettingResponse } from "../../lib/types";

export const getServiceFeeSetting = (branchId: number): Promise<ServiceFeeSettingResponse> =>
    api<ServiceFeeSettingResponse>(`/api/orders/service-fee-setting/branch/${branchId}`);

export const setServiceFeeEnabled = (branchId: number, enabled: boolean): Promise<void> =>
    api<void>("/api/orders/service-fee-setting", {
        method: "PUT",
        body: JSON.stringify({ branchId, enabled }),
    });

// Funcionário "dono" dos pedidos abertos pelo autoatendimento via QR Code
export const setSelfServiceEmployee = (branchId: number, employeeId: number | null): Promise<void> =>
    api<void>("/api/branches/self-service-employee", {
        method: "PUT",
        body: JSON.stringify({ branchId, employeeId }),
    });

export const getQrViewSetting = (branchId: number): Promise<{ enabled: boolean }> =>
    api<{ enabled: boolean }>(`/api/orders/qr-view-setting/branch/${branchId}`);

export const setQrViewEnabled = (branchId: number, enabled: boolean): Promise<void> =>
    api<void>("/api/orders/qr-view-setting", {
        method: "PUT",
        body: JSON.stringify({ branchId, enabled }),
    });

// Validação obrigatória na leitura da comanda/mesa: câmera, código de barras e QR Code.
export const getTableReadingValidationSetting = (branchId: number): Promise<TableReadingValidationSettingResponse> =>
    api<TableReadingValidationSettingResponse>(`/api/orders/table-reading-validation-setting/branch/${branchId}`);

export const setTableReadingValidation = (
    branchId: number,
    settings: { isCameraInputEnabled: boolean; isBarcodeEnabled: boolean; isQrCodeEnabled: boolean },
): Promise<void> =>
    api<void>("/api/orders/table-reading-validation-setting", {
        method: "PUT",
        body: JSON.stringify({ branchId, ...settings }),
    });