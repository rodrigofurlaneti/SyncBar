import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getIFoodMerchantStatus, getIFoodMerchantStatusByOperation } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { Button } from "../../ui/Button";
import { Tabs } from "../../components/Tabs";
import { PageHeader } from "../../components/PageHeader";
import { Alert } from "../../components/Alert";
import { QueryError } from "../../components/QueryError";
import { EmptyState } from "../../ui/EmptyState";
import { getMerchantStatusDisplay, getValidationStatusDisplay } from "../../utils/ifoodFormatters";

export function IFoodStatusPage() {
  const { branchId } = useAuthStore();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState("geral");

  const statusQuery = useQuery({
    queryKey: ["integrations", "ifood", "status", branchId],
    queryFn: () => getIFoodMerchantStatus(branchId),
    refetchInterval: 30000, // Recarrega a cada 30s
  });

  const statusByOpQuery = useQuery({
    queryKey: ["integrations", "ifood", "status-by-op", branchId],
    queryFn: () => getIFoodMerchantStatusByOperation(branchId),
    refetchInterval: 30000,
  });

  const data = statusQuery.data;
  const dataByOp = statusByOpQuery.data || [];

  if (statusQuery.isError) {
    return (
      <main style={{ padding: 22, maxWidth: 1200, margin: "0 auto" }}>
        <PageHeader
          title="Status da Loja - iFood"
          subtitle="Monitorar disponibilidade e validações"
          breadcrumb={[{ label: "iFood", href: "/integracoes/ifood" }]}
        />
        <QueryError error={statusQuery.error} what="o status da loja" />
      </main>
    );
  }

  const statusDisplay = data ? getMerchantStatusDisplay(data.available, data.operationState) : null;

  return (
    <main style={{ padding: 22, maxWidth: 1200, margin: "0 auto" }}>
      <PageHeader
        title="Status da Loja - iFood"
        subtitle="Monitorar disponibilidade e validações"
        breadcrumb={[{ label: "iFood", href: "/integracoes/ifood" }]}
        actions={
          <Button
            variant="ghost"
            onClick={() => statusQuery.refetch()}
            disabled={statusQuery.isRefetching}
          >
            🔄 {statusQuery.isRefetching ? "Atualizando..." : "Atualizar agora"}
          </Button>
        }
      />

      {statusQuery.isLoading ? (
        <div style={{ textAlign: "center", padding: "40px 20px" }}>
          <p style={{ color: "var(--ink-faint)" }}>Carregando status da loja...</p>
        </div>
      ) : !data ? (
        <EmptyState title="Sem status" description="Não foi possível obter o status da loja no iFood" />
      ) : (
        <>
          {/* Status Geral */}
          <div
            style={{
              padding: 24,
              borderRadius: 8,
              background: statusDisplay.bgColor,
              border: `2px solid ${statusDisplay.color}`,
              marginBottom: 24,
              display: "grid",
              gap: 16,
            }}
          >
            <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
              <div style={{ fontSize: "2rem" }}>{statusDisplay.icon}</div>
              <div>
                <p
                  style={{
                    fontSize: "0.9rem",
                    color: "var(--ink-faint)",
                    margin: "0 0 4px",
                    fontWeight: 600,
                  }}
                >
                  Estado da operação
                </p>
                <h2 style={{ fontSize: "1.5rem", margin: 0, color: statusDisplay.color }}>
                  {statusDisplay.label}
                </h2>
              </div>
            </div>

            {data.operationState && (
              <p style={{ margin: 0, fontSize: "0.9rem", color: statusDisplay.color }}>
                Estado: <strong>{data.operationState}</strong>
              </p>
            )}
          </div>

          {/* Validações */}
          {data.validations && data.validations.length > 0 && (
            <Tabs
              tabs={[
                { id: "geral", label: "Validações Gerais", badge: data.validations.length },
                { id: "por-op", label: "Por Operação", badge: dataByOp.length },
              ]}
              activeTab={activeTab}
              onTabChange={setActiveTab}
            >
              {activeTab === "geral" && (
                <div style={{ display: "grid", gap: 12 }}>
                  {data.validations.map((validation) => {
                    const display = getValidationStatusDisplay(validation.state, validation.message);
                    const severityColors = {
                      error: { bg: "#ffebee", border: "#f44336", text: "#b71c1c" },
                      warning: { bg: "#fff3e0", border: "#f57c00", text: "#e65100" },
                      info: { bg: "#e8f5e9", border: "#388e3c", text: "#1b5e20" },
                    };
                    const colors = severityColors[display.severity];

                    return (
                      <Alert
                        key={validation.id}
                        variant={display.severity}
                        title={validation.id}
                        message={
                          <>
                            <div>{display.label}</div>
                            {validation.message && (
                              <div style={{ fontSize: "0.85rem", marginTop: 4 }}>
                                {validation.message}
                              </div>
                            )}
                          </>
                        }
                      />
                    );
                  })}
                </div>
              )}

              {activeTab === "por-op" && (
                <div style={{ display: "grid", gap: 16 }}>
                  {dataByOp.length === 0 ? (
                    <EmptyState
                      title="Sem operações"
                      description="Nenhuma operação com dados de status disponível"
                    />
                  ) : (
                    dataByOp.map((op) => (
                      <div
                        key={op.operationType}
                        style={{
                          padding: 16,
                          borderRadius: 8,
                          background: "var(--surface-2)",
                          border: "1px solid var(--border)",
                        }}
                      >
                        <div
                          style={{
                            display: "flex",
                            gap: 12,
                            alignItems: "center",
                            marginBottom: 12,
                          }}
                        >
                          <div
                            style={{
                              width: 12,
                              height: 12,
                              borderRadius: "50%",
                              background: op.available ? "#4caf50" : "#f44336",
                            }}
                          />
                          <h3 style={{ margin: 0 }}>{op.operationType}</h3>
                          <span
                            style={{
                              fontSize: "0.85rem",
                              padding: "2px 8px",
                              borderRadius: 4,
                              background: op.available ? "#e8f5e9" : "#ffebee",
                              color: op.available ? "#1b5e20" : "#b71c1c",
                            }}
                          >
                            {op.available ? "✓ Disponível" : "✕ Indisponível"}
                          </span>
                        </div>

                        {op.validations && op.validations.length > 0 && (
                          <div style={{ display: "grid", gap: 8, marginTop: 12 }}>
                            {op.validations.map((v) => (
                              <div
                                key={v.id}
                                style={{
                                  padding: 8,
                                  borderRadius: 4,
                                  background: "var(--surface-1)",
                                  fontSize: "0.85rem",
                                  borderLeft: "2px solid var(--accent)",
                                }}
                              >
                                <strong>{v.id}:</strong> {v.state}
                                {v.message && <div style={{ marginTop: 4 }}>{v.message}</div>}
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                    ))
                  )}
                </div>
              )}
            </Tabs>
          )}

          {/* Rodapé com links para outras telas */}
          <div
            style={{
              marginTop: 32,
              padding: 16,
              borderRadius: 8,
              background: "var(--surface-2)",
              display: "grid",
              gap: 12,
            }}
          >
            <p style={{ margin: "0 0 8px", fontSize: "0.9rem", fontWeight: 600 }}>
              Mais informações
            </p>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))", gap: 8 }}>
              <Link to="/integracoes/ifood/pedidos" style={{ textDecoration: "none" }}>
                <Button variant="ghost" style={{ width: "100%" }}>
                  📦 Ver pedidos
                </Button>
              </Link>
              <Link to="/integracoes/ifood/avaliacoes" style={{ textDecoration: "none" }}>
                <Button variant="ghost" style={{ width: "100%" }}>
                  ⭐ Ver avaliações
                </Button>
              </Link>
              <Link to="/integracoes/ifood/financeiro/relatorios" style={{ textDecoration: "none" }}>
                <Button variant="ghost" style={{ width: "100%" }}>
                  💰 Ver financeiro
                </Button>
              </Link>
              <Link to="/integracoes/ifood/indicadores" style={{ textDecoration: "none" }}>
                <Button variant="ghost" style={{ width: "100%" }}>
                  📊 Ver indicadores
                </Button>
              </Link>
            </div>
          </div>
        </>
      )}
    </main>
  );
}
