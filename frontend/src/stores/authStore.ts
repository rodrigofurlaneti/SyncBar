import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { LoginResponse } from "../lib/types";

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  userName: string | null;
  companyId: number | null;
  employeeId: number | null;
  branchId: number;
  setSession: (session: LoginResponse) => void;
  setBranchId: (branchId: number) => void;
  clear: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      userName: null,
      companyId: null,
      employeeId: null,
      branchId: 1,
      setSession: (session) =>
        set({
          accessToken: session.accessToken,
          refreshToken: session.refreshToken,
          userName: session.userName,
          companyId: session.companyId,
          employeeId: session.employeeId,
        }),
      setBranchId: (branchId) => set({ branchId }),
      clear: () =>
        set({
          accessToken: null,
          refreshToken: null,
          userName: null,
          companyId: null,
          employeeId: null,
        }),
    }),
    { name: "syncbar-auth" },
  ),
);
