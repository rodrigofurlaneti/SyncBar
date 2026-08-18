import { Component } from "react";
import type { ErrorInfo, ReactNode } from "react";

interface Props {
  children: ReactNode;
}

interface State {
  error: Error | null;
}

/**
 * Rede de segurança para erros de render não tratados. Sem isso, um erro
 * em qualquer componente (ex.: acesso a propriedade de um dado que ainda
 * não chegou) derruba a árvore inteira do React e deixa uma tela branca —
 * péssimo num PDV em uso ativo no balcão.
 */
export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    // eslint-disable-next-line no-console
    console.error("[ErrorBoundary]", error, info.componentStack);
  }

  render() {
    if (this.state.error) {
      return (
        <div
          role="alert"
          style={{
            display: "grid",
            placeItems: "center",
            minHeight: "100vh",
            padding: 24,
            textAlign: "center",
            gap: 14,
            background: "var(--bg)",
            color: "var(--ink)",
          }}
        >
          <span className="display" style={{ fontSize: "2rem", color: "var(--danger)" }}>
            Algo deu errado
          </span>
          <p style={{ color: "var(--ink-dim)", maxWidth: 420, margin: 0 }}>
            A tela travou por um erro inesperado. Recarregar a página costuma resolver — se
            persistir, avise o suporte.
          </p>
          <button className="btn-primary" onClick={() => window.location.reload()}>
            Recarregar
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}
