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

  let response: Response;
  try {
    response = await fetch(path, {
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

  if (response.status === 401 && retry) {
    const renewed = await tryRefresh();
    if (renewed) return api<T>(path, init, false);
    throw new ApiError(401, "Auth.SessionExpired", "Sessão expirada. Entre novamente.");
  }

  if (!response.ok) {
    let title: string | undefined;
    let detail: string | undefined;
    try {
      const body = (await response.json()) as ApiProblem & { errors?: Record<string, string[]> };
      title = body.title;
      detail = body.detail;
      // ValidationProblemDetails (FluentValidation): agrega as mensagens de campo.
      if (!detail && body.errors)
        detail = Object.values(body.errors).flat().join(" ");
    } catch {
      // corpo vazio ou nao-JSON (ex.: 403 da policy de feature)
    }

    const fallback =
      response.status === 403
        ? "Você não tem acesso a esta funcionalidade — peça ao gerente na tela Acessos."
        : response.status === 404
          ? "Recurso não encontrado — a API está atualizada (reiniciada após a última alteração)?"
          : response.status >= 500
            ? "Erro interno na API — veja o console dela para detalhes."
            : "Falha ao comunicar com a API.";

    throw new ApiError(response.status, title ?? `Http.${response.status}`, detail ?? fallback);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}
