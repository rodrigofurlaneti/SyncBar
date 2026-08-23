import { useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getIFoodMerchantStatus, getIFoodMerchantStatusByOperation } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { Button } from "../../ui/Button";
import { PageHeader } from "../../components/PageHeader";
import { QueryError } from "../../components/QueryError";
import { EmptyState } from "../../ui/EmptyState";
import { Tabs } from "../../components/Tabs";
import { Alert } from "../../components/Alert";
import { DashboardCard } from "../../components/DashboardCard";
import { formatMerchantAvailability, formatValidationState } from "../../utils/ifoodFormattersEnhanced";

const OPERATION_LABELS: Record<string, string> = {
  DELIVERY: "🚗 Delivery",
  TAKEOUT: "🛍️ Retirada",
  DINE_IN: "🍽️ Consumo no local",
};

function formatOperationLabel(operation: string): string {
  return OPERATION_LABELS[operation] ?? operation;
}

export function IFoodStatusDetailedPage() {
  const { branchId } = useAuthStore();
  const [activeTab, setActiveTab] = useState("geral");
  const [selectedOperation, setSelectedOperation] = useState<string | null>(null);

  const statusQuery = useQuery({
    queryKey: ["integrations", "ifood", "status-detailed", branchId],
    queryFn: () => getIFoodMerchantStatus(branchId),
    refetchInterval: 30000,
  });

  const operationStatusQuery = useQuery({
    queryKey: ["integrations", "ifood", "status-by-operation", branchId, selectedOperation],
    queryFn: () => getIFoodMerchantStatusByOperation(branchId, selectedOperation!),
    enabled: !!selectedOperation,
  });

  const data = statusQuery.data;
  const operationData = operationStatusQuery.data;

  if (statusQuery.isLoading) {
    return (
      <main style={{ padding: 22, maxWidth: 1200, margin: "0 auto" }}>
        <PageHeader
          title="Status & Disponibilidade"
          subtitle="Detalhes completos e operações por tipo"
          breadcrumb={[{ label: "iFood", href: "/integracoes/ifood" }]}
        />
        <div style={{ textAlign: "center", padding: "40px 20px" }}>
          <p style={{ color: "var(--ink-faint)" }}>Carregando informações de status...</p>
        </div>
      </main>
    );
  }

  if (statusQuery.isError) {
    return (
      <main style={{ padding: 22, maxWidth: 1200, margin: "0 auto" }}>
        <PageHeader
          title="Status & Disponibilidade"
          subtitle="Detalhes completos e operações por tipo"
          breadcrumb={[{ label: "iFood", href: "/integracoes/ifood" }]}
        />
        <QueryError error={statusQuery.error} what="o status detalhado" />
      </main>
    );
  }

  if (!data) {
    return (
      <main style={{ padding: 22, maxWidth: 1200, margin: "0 auto" }}>
        <PageHeader
          title="Status & Disponibilidade"
          subtitle="Detalhes completos e operações por tipo"
          breadcrumb={[{ label: "iFood", href: "/integracoes/ifood" }]}
        />
        <EmptyState title="Sem status" description="Não foi possível obter informações de status da loja." />
      </main>
    );
  }

  const statusDisplay = formatMerchantAvailability(data.available, data.operationState);
  const validations = data.validations || [];
  // O `state` de cada validação vem bruto do iFood (vocabulário não documentado) — a severidade
  // é resolvida por formatValidationState, que cobre as grafias conhecidas.
  const errorValidations = validations.filter((v) => formatValidationState(v.state).severity === "error");
  const warningValidations = validations.filter((v) => formatValidationState(v.state).severity === "warning");

  // O iFood não expõe a lista de operações habilitadas da loja; o endpoint de status por
  // operação é consultado sob demanda para cada uma das operações possíveis.
  const availableOperations = ["DELIVERY", "TAKEOUT", "DINE_IN"];

  return (
    <main style={{ padding: 22, maxWidth: 1200, margin: "0 auto" }}>
      <PageHeader
        title="Status & Disponibilidade"
        subtitle="Detalhes completos e operações por tipo"
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

      {/* Card Principal de Status */}
      <div
        style={{
          padding: 24,
          borderRadius: 8,
          background: statusDisplay.bg,
          border: `3px solid ${statusDisplay.color}`,
          marginBottom: 24,
          display: "grid",
          gap: 16,
        }}
      >
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
          <div style={{ display: "flex", gap: 16, alignItems: "center" }}>
            <div style={{ fontSize: "3rem" }}>{statusDisplay.icon}</div>
            <div>
              <p
                style={{
                  fontSize: "0.9rem",
                  color: statusDisplay.color,
                  margin: "0 0 4px",
                  fontWeight: 600,
                  opacity: 0.8,
                }}
              >
                ESTADO GERAL
              </p>
              <h1 style={{ fontSize: "2.2rem", margin: 0, color: statusDisplay.color, fontWeight: 800 }}>
                {statusDisplay.label}
              </h1>
              {data.operationState && (
                <p
                  style={{
                    fontSize: "0.95rem",
                    color: statusDisplay.color,
                    margin: "8px 0 0",
                    opacity: 0.9,
                    fontWeight: 500,
                  }}
                >
                  {data.operationState}
                </p>
              )}
            </div>
          </div>

          {/* A API do iFood não tem liga/desliga de disponibilidade: pausar a loja é criar uma
              interrupção, gerenciada na tela da integração. */}
          <Link to="/integracoes/ifood" style={{ textDecoration: "none" }}>
            <Button variant="ghost">⏸️ Gerenciar interrupções</Button>
          </Link>
        </div>

        {/* Resumo de Validações */}
        {(errorValidations.length > 0 || warningValidations.length > 0) && (
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))", gap: 12 }}>
            {errorValidations.length > 0 && (
              <div
                style={{
                  padding: 12,
                  borderRadius: 6,
                  background: "rgba(0,0,0,0.1)",
                  borderLeft: "3px solid #ef4444",
                }}
              >
                <p style={{ fontSize: "0.85rem", margin: 0, fontWeight: 600, color: statusDisplay.color }}>
                  ✕ {errorValidations.length} Erro{errorValidations.length > 1 ? "s" : ""}
                </p>
              </div>
            )}
            {warningValidations.length > 0 && (
              <div
                style={{
                  padding: 12,
                  borderRadius: 6,
                  background: "rgba(0,0,0,0.1)",
                  borderLeft: "3px solid #f59e0b",
                }}
              >
                <p style={{ fontSize: "0.85rem", margin: 0, fontWeight: 600, color: statusDisplay.color }}>
                  ⚠ {warningValidations.length} Aviso{warningValidations.length > 1 ? "s" : ""}
                </p>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Abas: Validações Gerais vs Por Operação */}
      <Tabs
        tabs={[
          { id: "geral", label: "Validações Gerais", badge: validations.length },
          { id: "operacoes", label: "Por Operação", badge: availableOperations.length },
        ]}
        activeTab={activeTab}
        onTabChange={setActiveTab}
      >
        {/* ABA 1: Validações Gerais */}
        {activeTab === "geral" && (
          <div style={{ display: "grid", gap: 12, marginTop: 16 }}>
            {validations.length === 0 ? (
              <div className="card" style={{ padding: 32, textAlign: "center" }}>
                <p style={{ fontSize: "1.1rem", fontWeight: 600, color: "var(--ink-faint)", margin: 0 }}>
                  ✓ Tudo certo! Nenhuma validação pendente.
                </p>
              </div>
            ) : (
              validations.map((validation) => {
                const display = formatValidationState(validation.state);
                return (
                  <Alert
                    key={validation.id}
                    variant={display.severity}
                    title={validation.id}
                    message={
                      <div>
                        <div style={{ fontWeight: 600, marginBottom: 4 }}>
                          {display.icon} {display.label}
                        </div>
                        {validation.message && (
                          <div style={{ fontSize: "0.9rem", marginTop: 8, opacity: 0.9 }}>
                            {validation.message}
                          </div>
                        )}
                      </div>
                    }
                  />
                );
              })
            )}
          </div>
        )}

        {/* ABA 2: Por Operação */}
        {activeTab === "operacoes" && (
          <div style={{ marginTop: 16 }}>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))", gap: 12, marginBottom: 24 }}>
              {availableOperations.map((op) => (
                <button
                  key={op}
                  onClick={() => setSelectedOperation(op)}
                  style={{
                    padding: 12,
                    borderRadius: 8,
                    border: selectedOperation === op ? "2px solid var(--accent)" : "1px solid var(--border)",
                    background: selectedOperation === op ? "var(--surface-2)" : "var(--surface-1)",
                    cursor: "pointer",
                    fontSize: "0.95rem",
                    fontWeight: selectedOperation === op ? 700 : 500,
                    transition: "all 0.2s",
                  }}
                >
                  {formatOperationLabel(op)}
                </button>
              ))}
            </div>

            {selectedOperation && (
              <>
                {operationStatusQuery.isLoading ? (
                  <div style={{ textAlign: "center", padding: "20px" }}>
                    <p style={{ color: "var(--ink-faint)" }}>Carregando detalhes da operação...</p>
                  </div>
                ) : operationStatusQuery.isError ? (
                  <QueryError error={operationStatusQuery.error} what={`status da operação ${selectedOperation}`} />
                ) : operationData ? (
                  <div style={{ display: "grid", gap: 16 }}>
                    {/* Card de Status */}
                    <DashboardCard
                      title={`${formatOperationLabel(selectedOperation)} - Status`}
                      value={operationData.available ? "Disponível" : "Indisponível"}
                      status={operationData.available ? "success" : "error"}
                      icon={operationData.available ? "✓" : "✕"}
                      subtitle={operationData.state || "—"}
                    />

                    {/* Validações da Operação */}
                    {operationData.validations && operationData.validations.length > 0 && (
                      <div className="card" style={{ padding: 16 }}>
                        <h4 style={{ fontSize: "0.95rem", fontWeight: 700, margin: "0 0 12px" }}>
                          Validações - {formatOperationLabel(selectedOperation)}
                        </h4>
                        <div style={{ display: "grid", gap: 8 }}>
                          {operationData.validations.map((v) => {
                            const display = formatValidationState(v.state);
                            return (
                              <Alert
                                key={v.id}
                                variant={display.severity}
                                title={v.id}
                                message={
                                  <>
                                    <div style={{ fontWeight: 600 }}>
                                      {display.icon} {display.label}
                                    </div>
                                    {v.message && (
                                      <div style={{ fontSize: "0.85rem", marginTop: 4 }}>
                                        {v.message}
                                      </div>
                                    )}
                                  </>
                                }
                              />
                            );
                          })}
                        </div>
                      </div>
                    )}
                  </div>
                ) : null}
              </>
            )}
          </div>
        )}
      </Tabs>

      {/* Footer com ações */}
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
          Outras operações
        </p>
        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))",
            gap: 8,
          }}
        >
          <Link to="/integracoes/ifood/pedidos" style={{ textDecoration: "none" }}>
            <Button variant="ghost" style={{ width: "100%" }}>
              📦 Gerenciar Pedidos
            </Button>
          </Link>
          <Link to="/integracoes/ifood/financeiro/relatorios" style={{ textDecoration: "none" }}>
            <Button variant="ghost" style={{ width: "100%" }}>
              💰 Financeiro
            </Button>
          </Link>
          <Link to="/integracoes/ifood/avaliacoes" style={{ textDecoration: "none" }}>
            <Button variant="ghost" style={{ width: "100%" }}>
              ⭐ Avaliações
            </Button>
          </Link>
        </div>
      </div>
    </main>
  );
}
