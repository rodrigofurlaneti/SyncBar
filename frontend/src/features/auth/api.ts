import { api } from "../../lib/apiClient";
import type { LoginResponse } from "../../lib/types";

export const login = (userName: string, password: string): Promise<LoginResponse> =>
  api<LoginResponse>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ userName, password }),
  });
