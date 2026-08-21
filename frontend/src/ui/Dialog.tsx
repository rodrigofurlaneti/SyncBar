import type { ReactNode } from "react";
import { swal, swalClasses } from "../lib/swal";

interface ConfirmOptions {
  title?: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;
}

interface PromptOptions {
  title?: string;
  message?: string;
  label: string;
  defaultValue?: string;
  placeholder?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  inputMode?: "text" | "decimal" | "numeric";
}

interface DialogApi {
  confirm: (options: ConfirmOptions) => Promise<boolean>;
  prompt: (options: PromptOptions) => Promise<string | null>;
}

const dialogApi: DialogApi = {
  async confirm(options) {
    const result = await swal.fire({
      title: options.title ?? "Confirmar",
      text: options.message,
      icon: options.danger ? "warning" : "question",
      showCancelButton: true,
      confirmButtonText: options.confirmLabel ?? "Confirmar",
      cancelButtonText: options.cancelLabel ?? "Cancelar",
      customClass: {
        ...swalClasses,
        confirmButton: options.danger ? "btn-danger" : "btn-primary",
      },
    });
    return result.isConfirmed;
  },

  async prompt(options) {
    const result = await swal.fire({
      title: options.title ?? "Informe um valor",
      text: options.message,
      input: "text",
      inputLabel: options.label,
      inputValue: options.defaultValue ?? "",
      inputPlaceholder: options.placeholder,
      inputAttributes: {
        inputmode: options.inputMode ?? "text",
        autocapitalize: "off",
      },
      showCancelButton: true,
      confirmButtonText: options.confirmLabel ?? "Confirmar",
      cancelButtonText: options.cancelLabel ?? "Cancelar",
    });
    // Confirmar devolve o texto (mesmo vazio); Cancelar/Esc/fora devolve null.
    if (!result.isConfirmed) return null;
    return (result.value as string | undefined) ?? "";
  },
};

/**
 * O SweetAlert2 gerencia seu próprio overlay/portal fora da árvore do React
 * (foco preso, Esc, retorno de foco — tudo nativo), então não precisa mais de
 * estado React nem de <Modal>. Mantido como passthrough só para não quebrar
 * quem ainda monta <DialogProvider> em main.tsx.
 */
export function DialogProvider({ children }: { children: ReactNode }) {
  return <>{children}</>;
}

export function useDialog(): DialogApi {
  return dialogApi;
}
