import type { ReactNode } from "react";
import { swalToast } from "../lib/swal";

type ToastTone = "success" | "error" | "info";

interface ToastApi {
  success: (message: string) => void;
  error: (message: string) => void;
  info: (message: string) => void;
}

const iconByTone: Record<ToastTone, "success" | "error" | "info"> = {
  success: "success",
  error: "error",
  info: "info",
};

function push(tone: ToastTone, message: string) {
  void swalToast.fire({
    icon: iconByTone[tone],
    title: message,
    timer: tone === "error" ? 6000 : 3500,
    customClass: {
      popup: `sb-swal-toast sb-swal-toast--${tone}`,
    },
  });
}

const toastApi: ToastApi = {
  success: (m) => push("success", m),
  error: (m) => push("error", m),
  info: (m) => push("info", m),
};

/**
 * Os toasts do SweetAlert2 se empilham e se removem sozinhos fora da árvore
 * do React — não precisa mais de estado/Provider. Mantido como passthrough só
 * para não quebrar quem ainda monta <ToastProvider> em main.tsx.
 */
export function ToastProvider({ children }: { children: ReactNode }) {
  return <>{children}</>;
}

export function useToast(): ToastApi {
  return toastApi;
}
