import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { featurePath, useMyFeatures } from "./hooks";

interface Props {
  code: string;
  children: ReactNode;
}

// Esconde a rota de quem nao tem a tela liberada — o backend tambem bloqueia
// via policy Feature:X; isto aqui e so a experiencia de navegacao.
export function FeatureGate({ code, children }: Props) {
  const featuresQuery = useMyFeatures();

  if (featuresQuery.isLoading) return null;

  const data = featuresQuery.data;
  const allowed = data?.canManageAccess || data?.features.includes(code);
  if (allowed) return <>{children}</>;

  const firstAllowed = data?.features.find((f) => featurePath[f] !== undefined);
  return <Navigate to={firstAllowed ? featurePath[firstAllowed] : "/sem-acesso"} replace />;
}

export function NoAccessPage() {
  return (
    <main style={{ display: "grid", placeItems: "center", minHeight: "60vh" }}>
      <div style={{ textAlign: "center", display: "grid", gap: 8 }}>
        <span className="display" style={{ fontSize: "2rem" }}>Sem telas liberadas</span>
        <span style={{ color: "var(--ink-dim)" }}>
          Peça ao gerente para conceder acesso na tela Acessos.
        </span>
      </div>
    </main>
  );
}
