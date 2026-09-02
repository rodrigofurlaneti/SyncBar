import { useAuthStore } from "../stores/authStore";
import type { ApiProblem, LoginResponse } from "./types";

export class ApiError extends Error {
    constructor(
        public readonly status: number,
        public readonly code: string,
        message: string,
    ) {
        super(message);
    }
}

let refreshing: Promise<boolean> | null = null;

async function tryRefresh(): Promise<boolean> {
    refreshing ??= (async () => {
        const { refreshToken, setSession, clear } = useAuthStore.getState();
        if (!refreshToken) return false;
        try {
            const response = await fetch("/api/auth/refresh", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ refreshToken }),
            });
            if (!response.ok) {
                clear();
                return false;
            }
            setSession((await response.json()) as LoginResponse);
            return true;
        } catch {
            clear();
            return false;
        } finally {
            refreshing = null;
        }
    })();
    return refreshing;
}

export async function apiUpload<T>(path: string, formData: FormData, retry = true): Promise<T> {
    const { accessToken } = useAuthStore.getState();

    let response: Response;
    try {
        response = await fetch(path, {
            method: "POST",
            headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {},
            body: formData,
        });
    } catch {
        throw new ApiError(0, "Network.Unreachable", "Não foi possível conectar à API — ela está rodando?");
    }

    if (response.status === 401 && retry) {
        const renewed = await tryRefresh();
        if (renewed) return apiUpload<T>(path, formData, false);
        throw new ApiError(401, "Auth.SessionExpired", "Sessão expirada. Entre novamente.");
    }

    if (!response.ok) {
        let title: string | undefined;
        let detail: string | undefined;
        try {
            const body = (await response.json()) as ApiProblem & { errors?: Record<string, string[]> };
            title = body.title;
            detail = body.detail ?? (body.errors ? Object.values(body.errors).flat().join(" ") : undefined);
        } catch { /* corpo vazio */ }
        throw new ApiError(response.status, title ?? `Http.${response.status}`, detail ?? "Falha ao enviar o arquivo.");
    }

    if (response.status === 204) return undefined as T;
    return (await response.json()) as T;
}

async function fetchJson(path: string, init: RequestInit | undefined, accessToken: string | null): Promise<Response> {
    try {
        return await fetch(path, {
            ...init,
            headers: {
                "Content-Type": "application/json",
                ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
                ...init?.headers,
            },
        });
    } catch {
        throw new ApiError(0, "Network.Unreachable", "Não foi possível conectar à API — ela está rodando?");
    }
}

async function parseErrorBody(response: Response): Promise<{ title?: string; detail?: string }> {
    try {
        const body = (await response.json()) as ApiProblem & { errors?: Record<string, string[]> };
        const detail = body.detail ?? (body.errors ? Object.values(body.errors).flat().join(" ") : undefined);
        return { title: body.title, detail };
    } catch {
        return {};
    }
}

function getFallbackMessage(status: number): string {
    if (status === 403) return "Você não tem acesso a esta funcionalidade — peça ao gerente na tela Acessos.";
    if (status === 404) return "Recurso não encontrado — a API está atualizada (reiniciada após a última alteração)?";
    if (status >= 500) return "Erro interno na API — veja o console dela para detalhes.";
    return "Falha ao comunicar com a API.";
}

async function handleUnauthorized<T>(retryFn: () => Promise<T>): Promise<T> {
    const renewed = await tryRefresh();
    if (renewed) return retryFn();
    throw new ApiError(401, "Auth.SessionExpired", "Sessão expirada. Entre novamente.");
}

export async function api<T>(path: string, init?: RequestInit, retry = true): Promise<T> {
    const { accessToken } = useAuthStore.getState();

    const response = await fetchJson(path, init, accessToken);

    if (response.status === 401 && retry) {
        return handleUnauthorized(() => api<T>(path, init, false));
    }

    if (!response.ok) {
        const { title, detail } = await parseErrorBody(response);
        throw new ApiError(response.status, title ?? `Http.${response.status}`, detail ?? getFallbackMessage(response.status));
    }

    if (response.status === 204) return undefined as T;
    return (await response.json()) as T;
}