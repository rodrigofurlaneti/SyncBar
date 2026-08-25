import { api } from "../../lib/apiClient";

export interface DiningAreaResponse {
    id: number;
    name: string;
    isActive: boolean;
}

export interface CreateDiningAreaPayload {
    branchId: number;
    name: string;
}

export const getDiningAreasByBranch = (branchId: number): Promise<DiningAreaResponse[]> =>
    api<DiningAreaResponse[]>(`/api/diningareas/branch/${branchId}`);

export const getDiningAreaById = (id: number): Promise<DiningAreaResponse> =>
    api<DiningAreaResponse>(`/api/diningareas/${id}`);

export const createDiningArea = (payload: CreateDiningAreaPayload): Promise<number> =>
    api<number>("/api/diningareas", { method: "POST", body: JSON.stringify(payload) });

export const updateDiningArea = (id: number, name: string): Promise<void> =>
    api<void>(`/api/diningareas/${id}`, { method: "PUT", body: JSON.stringify({ name }) });

export interface DiningAreaTableResponse {
    id: number;
    diningTableId: number;
    isActive: boolean;
}

export const getTablesByArea = (areaId: number): Promise<DiningAreaTableResponse[]> =>
    api<DiningAreaTableResponse[]>(`/api/diningareas/${areaId}/tables`);

export const assignTableToArea = (areaId: number, diningTableId: number): Promise<number> =>
    api<number>(`/api/diningareas/${areaId}/tables`, {
        method: "POST",
        body: JSON.stringify({ diningTableId })
    });

export const removeTableFromArea = (assignmentId: number): Promise<void> =>
    api<void>(`/api/diningareas/tables/${assignmentId}`, { method: "DELETE" });

export interface DiningAreaAssignmentResponse {
    id: number;
    diningAreaId: number;
    employeeId: number;
    startAt: string;
}

export const getActiveAssignmentsByArea = (areaId: number): Promise<DiningAreaAssignmentResponse[]> =>
    api<DiningAreaAssignmentResponse[]>(`/api/diningareas/${areaId}/assignments/active`);

export const getActiveAssignmentsByEmployee = (employeeId: number): Promise<DiningAreaAssignmentResponse[]> =>
    api<DiningAreaAssignmentResponse[]>(`/api/diningareas/assignments/employee/${employeeId}/active`);

export const startAssignment = (areaId: number, employeeId: number, startAt: string): Promise<number> =>
    api<number>(`/api/diningareas/${areaId}/assignments`, {
        method: "POST",
        body: JSON.stringify({ employeeId, startAt })
    });

export const endAssignment = (assignmentId: number, endAt: string): Promise<void> =>
    api<void>(`/api/diningareas/assignments/${assignmentId}/end`, {
        method: "PUT",
        body: JSON.stringify({ endAt })
    });