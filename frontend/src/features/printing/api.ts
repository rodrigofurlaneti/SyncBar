import { api } from "../../lib/apiClient";
import type { PrinterResponse, PrintSettingsResponse } from "../../lib/types";

export const getPrintSettings = (branchId: number): Promise<PrintSettingsResponse> =>
  api<PrintSettingsResponse>(`/api/printing/settings/branch/${branchId}`);

export const setPrintSettings = (payload: {
  branchId: number;
  printOrdersEnabled: boolean;
  printBillsEnabled: boolean;
}): Promise<void> =>
  api<void>("/api/printers/settings", { method: "PUT", body: JSON.stringify(payload) });

export const getPrinters = (branchId: number): Promise<PrinterResponse[]> =>
  api<PrinterResponse[]>(`/api/printers/branch/${branchId}`);

export const createPrinter = (payload: {
  branchId: number;
  name: string;
  connectionType: number;
  printerName: string | null;
  ipAddress: string | null;
  port: number | null;
  printsOrders: boolean;
  printsBills: boolean;
}): Promise<number> =>
  api<number>("/api/printers", { method: "POST", body: JSON.stringify(payload) });

export const deactivatePrinter = (id: number): Promise<void> =>
  api<void>(`/api/printers/${id}/deactivate`, { method: "PUT" });

export const testPrinter = (id: number): Promise<void> =>
  api<void>(`/api/printers/${id}/test`, { method: "POST" });

export const printBill = (orderId: number): Promise<void> =>
  api<void>(`/api/printing/bill/${orderId}`, { method: "POST" });

export const printCashClosing = (sessionId: number): Promise<void> =>
  api<void>(`/api/printing/cash-session/${sessionId}`, { method: "POST" });
