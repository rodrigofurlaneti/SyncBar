import type { ReactNode } from "react";

interface Props {
  icon?: ReactNode;
  title: string;
  description?: string;
  action?: ReactNode;
}

/**
 * Estado vazio com hierarquia clara + CTA opcional. Usar no lugar de uma
 * única linha de texto cinza ("Nenhum X cadastrado.") sempre que uma lista
 * ou consulta voltar vazia — dá contexto e um caminho de ação, em vez de
 * deixar a pessoa sem saber o que fazer a seguir.
 */
export function EmptyState({ icon, title, description, action }: Props) {
  return (
    <div
      className="rise"
      style={{
        display: "grid",
        placeItems: "center",
        textAlign: "center",
        gap: 10,
        padding: "48px 24px",
        color: "var(--ink-faint)",
      }}
    >
      {icon && (
        <div aria-hidden="true" style={{ fontSize: "2.4rem", lineHeight: 1 }}>
          {icon}
        </div>
      )}
      <span className="display" style={{ fontSize: "1.3rem", color: "var(--ink)" }}>
        {title}
      </span>
      {description && <span style={{ maxWidth: 360 }}>{description}</span>}
      {action && <div style={{ marginTop: 6 }}>{action}</div>}
    </div>
  );
}
