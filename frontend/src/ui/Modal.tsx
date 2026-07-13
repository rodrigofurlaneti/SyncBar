import { useEffect, useId, useRef } from "react";
import type { ReactNode } from "react";
import { Button } from "./Button";

interface Props {
  onClose: () => void;
  children: ReactNode;
  title?: ReactNode;
  variant?: "center" | "drawer";
  wide?: boolean;
  /** fecha ao clicar no fundo (default: true) */
  dismissable?: boolean;
  /** rótulo acessível quando não há title visível */
  ariaLabel?: string;
}

const FOCUSABLE =
  'a[href], button:not([disabled]), textarea, input, select, [tabindex]:not([tabindex="-1"])';

/**
 * Modal/Drawer acessível: role="dialog", aria-modal, trap de foco,
 * Esc para fechar, retorno de foco ao elemento anterior e scroll lock.
 */
export function Modal({
  onClose,
  children,
  title,
  variant = "center",
  wide = false,
  dismissable = true,
  ariaLabel,
}: Props) {
  const panelRef = useRef<HTMLDivElement>(null);
  const titleId = useId();
  const isDrawer = variant === "drawer";

  useEffect(() => {
    const previouslyFocused = document.activeElement as HTMLElement | null;
    const panel = panelRef.current;

    // foca o primeiro elemento focável (ou o painel)
    const focusables = panel?.querySelectorAll<HTMLElement>(FOCUSABLE);
    (focusables && focusables.length ? focusables[0] : panel)?.focus();

    // scroll lock
    const prevOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        e.stopPropagation();
        onClose();
        return;
      }
      if (e.key === "Tab" && panel) {
        const items = Array.from(panel.querySelectorAll<HTMLElement>(FOCUSABLE)).filter(
          (el) => el.offsetParent !== null,
        );
        if (items.length === 0) {
          e.preventDefault();
          return;
        }
        const first = items[0];
        const last = items[items.length - 1];
        const active = document.activeElement as HTMLElement;
        if (e.shiftKey && active === first) {
          e.preventDefault();
          last.focus();
        } else if (!e.shiftKey && active === last) {
          e.preventDefault();
          first.focus();
        }
      }
    };

    document.addEventListener("keydown", onKeyDown, true);
    return () => {
      document.removeEventListener("keydown", onKeyDown, true);
      document.body.style.overflow = prevOverflow;
      previouslyFocused?.focus?.();
    };
  }, [onClose]);

  return (
    <div
      className={`modal-backdrop ${isDrawer ? "is-drawer" : "is-center"}`}
      onMouseDown={(e) => {
        if (dismissable && e.target === e.currentTarget) onClose();
      }}
    >
      <div
        ref={panelRef}
        className={`modal-panel rise ${isDrawer ? "is-drawer" : "is-center"} ${wide ? "is-wide" : ""}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby={title ? titleId : undefined}
        aria-label={title ? undefined : ariaLabel}
        tabIndex={-1}
      >
        {title && (
          <div className="modal-head">
            <h3 id={titleId} className="display" style={{ fontSize: "1.5rem" }}>
              {title}
            </h3>
            <Button iconOnly aria-label="Fechar" size="sm" onClick={onClose}>
              ✕
            </Button>
          </div>
        )}
        {children}
      </div>
    </div>
  );
}
