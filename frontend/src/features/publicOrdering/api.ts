// Chamadas sem autenticação — o "segredo" é o token do QR Code da mesa.
// Não usa lib/apiClient (que injeta Authorization e tenta refresh de sessão).

import type { PublicMenuResponse } from "../../lib/types";

async function publicApi<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: { "Content-Type": "application/json", ...init?.headers },
  });
  if (!response.ok) {
    let detail: string | undefined;
    try {
      const body = (await response.json()) as { detail?: string; title?: string };
      detail = body.detail ?? body.title;
    } catch { /* corpo vazio */ }
    throw new Error(detail ?? "Não foi possível completar o pedido.");
  }
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export const getPublicMenu = (token: string): Promise<PublicMenuResponse> =>
  publicApi<PublicMenuResponse>(`/api/publicordering/${token}/menu`);

export const addPublicOrderItem = (
  token: string,
  productId: number,
  quantity: number,
  notes: string | null,
): Promise<{ orderId: number }> =>
  publicApi<{ orderId: number }>(`/api/publicordering/${token}/items`, {
    method: "POST",
    body: JSON.stringify({ productId, quantity, notes }),
  });
