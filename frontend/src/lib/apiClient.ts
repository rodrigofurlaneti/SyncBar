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
  // Uma unica renovacao por vez — demais chamadas aguardam a mesma promise.
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

export async function api<T>(path: string, init?: RequestInit, retry = true): Promise<T> {
  const { accessToken } = useAuthStore.getState();

  const response = await fetch(path, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...init?.headers,
    },
  });

  if (response.status === 401 && retry) {
    const renewed = await tryRefresh();
    if (renewed) return api<T>(path, init, false);
    throw new ApiError(401, "Auth.SessionExpired", "Sessão expirada. Entre novamente.");
  }

  if (!response.ok) {
    let problem: ApiProblem | null = null;
    try {
      problem = (await response.json()) as ApiProblem;
    } catch {
      // corpo vazio ou nao-JSON
    }
    throw new ApiError(
      response.status,
      problem?.title ?? `Http.${response.status}`,
      problem?.detail ?? "Falha ao comunicar com a API.",
    );
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}
