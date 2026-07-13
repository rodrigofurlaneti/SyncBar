import type { CSSProperties, ReactNode } from "react";

interface Props {
  /** cor do ponto/estado (token CSS, ex.: "var(--free)") */
  color?: string;
  children: ReactNode;
  title?: string;
}

/**
 * Selo de status — nunca comunica só por cor: o rótulo textual acompanha
 * o ponto colorido (acessível para daltônicos).
 */
export function StatusBadge({ color = "var(--ink-faint)", children, title }: Props) {
  return (
    <span className="chip" style={{ "--dot": color } as CSSProperties} title={title}>
      {children}
    </span>
  );
}
