import Swal from "sweetalert2";

/**
 * Classes usadas em todo alerta/confirmação/prompt do SweetAlert2 — reaproveitam
 * os tokens e as classes de botão (.btn-primary/.btn-ghost/.btn-danger) já
 * existentes no design system, para o SweetAlert2 não parecer um plugin colado
 * fora do tema "quadro de bar" (âmbar sobre carvão / papel de comanda no claro).
 * Veja a seção "SweetAlert2" em global.css para os estilos correspondentes.
 */
export const swalClasses = {
  container: "sb-swal-container",
  popup: "sb-swal-popup",
  title: "sb-swal-title",
  htmlContainer: "sb-swal-html",
  actions: "sb-swal-actions",
  confirmButton: "btn-primary",
  cancelButton: "btn-ghost",
  denyButton: "btn-danger",
  input: "sb-swal-input",
  icon: "sb-swal-icon",
} as const;

// Instância para confirmações/prompts (com overlay, foco preso, Esc — o
// SweetAlert2 já cuida de tudo isso nativamente).
export const swal = Swal.mixin({
  buttonsStyling: false,
  reverseButtons: true,
  focusConfirm: false,
  allowOutsideClick: () => !Swal.isLoading(),
  customClass: swalClasses,
  showClass: { popup: "sb-swal-show" },
  hideClass: { popup: "sb-swal-hide" },
});

// Instância separada para toasts (canto da tela, sem overlay, timer automático,
// pausa no hover) — substitui o ToastProvider/Toast.tsx antigo.
export const swalToast = Swal.mixin({
  toast: true,
  position: "top-end",
  showConfirmButton: false,
  timerProgressBar: true,
  buttonsStyling: false,
  didOpen: (toastEl) => {
    toastEl.addEventListener("mouseenter", Swal.stopTimer);
    toastEl.addEventListener("mouseleave", Swal.resumeTimer);
  },
});
