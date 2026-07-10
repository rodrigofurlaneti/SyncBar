import { ApiError } from "../lib/apiClient";

interface Props {
  error: unknown;
  what: string;
}

// Exibe falhas de carregamento de forma consistente em todas as paginas.
export function QueryError({ error, what }: Props) {
  const message =
    error instanceof ApiError ? error.message : `Falha ao carregar ${what}.`;
  return (
    <p className="error-text">
      Falha ao carregar {what}: {message}
    </p>
  );
}
