import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getIFoodMerchantStatus, getIFoodOrders, getIFoodFinancialSummary, getIFoodReviews } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { PageHeader } from "../../components/PageHeader";
import { QueryError } from "../../components/QueryError";
import { Button } from "../../ui/Button";
import { DashboardCard } from "../../components/DashboardCard";
import { StatsGrid } from "../../components/StatsGrid";
import { MetricsRow } from "../../components/MetricsRow";
import {
  formatOrderStatus,
  formatMerchantAvailability,
  formatCurrency,
  calculateOrderMetrics,
  formatTime,
} from "../../utils/ifoodFormattersEnhanced";

const REVIEWS_PAGE_SIZE = 50;

const ORDER_TYPE_LABELS: Record<string, string> = {
  DELIVERY: "🚗 Delivery",
  TAKEOUT: "🛍️ Retirada",
};

export function IFoodDashboardPage() {
  const { branchId } = useAuthStore();

  // Queries em paralelo
  const statusQuery = useQuery({
    queryKey: ["integrations", "ifood", "dashboard", "status", branchId],
    queryFn: () => getIFoodMerchantStatus(branchId),
    refetchInterval: 30000,
    // Fase 20 (2026-08-24): sem isso, o retry padrão do TanStack Query (3 tentativas com
    // backoff) transformava uma única falha em várias chamadas reais ao iFood em rajada —
    // o próximo poll de 30s já tenta de novo, não precisa de retry automático aqui.
    retry: false,
  });

  const ordersQuery = useQuery({
    queryKey: ["integrations", "ifood", "dashboard", "orders", branchId],
    queryFn: () => getIFoodOrders(branchId),
    refetchInterval: 15000,
  });

  const financialQuery = useQuery({
    queryKey: ["integrations", "ifood", "dashboard", "financial", branchId],
    queryFn: () => getIFoodFinancialSummary(branchId),
    refetchInterval: 60000,
  });

  const reviewsQuery = useQuery({
    queryKey: ["integrations", "ifood", "dashboard", "reviews", branchId],
    queryFn: () => getIFoodReviews(branchId, { pageSize: REVIEWS_PAGE_SIZE }),
    refetchInterval: 60000,
  });

  const isLoading = statusQuery.isLoading || ordersQuery.isLoading;
  const isError = statusQuery.isError || ordersQuery.isError;

  const merchantStatus = statusQuery.data;
  const orders = ordersQuery.data || [];
  const financial = financialQuery.data;
  const reviews = reviewsQuery.data?.reviews ?? [];

  const metrics = calculateOrderMetrics(orders);
  const statusDisplay = merchantStatus ? formatMerchantAvailability(merchantStatus.available, merchantStatus.operationState) : null;

  // Estatísticas de reviews — a lista do iFood não traz um estado de resposta; "respondida" é
  // simplesmente a avaliação que já tem `reply` preenchido.
  const scoredReviews = reviews.filter((r) => r.score !== null);
  const avgRating =
    scoredReviews.length > 0
      ? (scoredReviews.reduce((sum, r) => sum + (r.score ?? 0), 0) / scoredReviews.length).toFixed(1)
      : "—";
  const respondedReviews = reviews.filter((r) => !!r.reply).length;

  const primaryError = statusQuery.isError ? statusQuery.error : ordersQuery.error;
  const errorMessage = isError ? primaryError : null;

  return (
    <main style={{ padding: 22, maxWidth: 1400, margin: "0 auto" }}>
      {/* Header */}
      <PageHeader
        title="Dashboard iFood"
        subtitle="Visão centralizada de performance, pedidos e operação"
        breadcrumb={[{ label: "Integrações", href: "/integracoes/ifood" }]}
        actions={
          <Button
            variant="ghost"
            onClick={() => {
              statusQuery.refetch();
              ordersQuery.refetch();
              financialQuery.refetch();
              reviewsQuery.refetch();
            }}
            disabled={isLoading}
          >
            🔄 {isLoading ? "Atualizando..." : "Atualizar agora"}
          </Button>
        }
      />

      {/* Status de disponibilidade */}
      {statusDisplay && (
        <div
          style={{
            padding: 16,
            borderRadius: 8,
            background: statusDisplay.bg,
            border: `2px solid ${statusDisplay.color}`,
            marginBottom: 24,
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
          }}
        >
          <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
            <div style={{ fontSize: "2rem" }}>{statusDisplay.icon}</div>
            <div>
              <p style={{ fontSize: "0.9rem", color: statusDisplay.color, margin: "0 0 4px", fontWeight: 600 }}>
                Estado da Operação
              </p>
              <h2 style={{ fontSize: "1.5rem", margin: 0, color: statusDisplay.color }}>
                {statusDisplay.label}
              </h2>
              {merchantStatus?.operationState && (
                <p style={{ fontSize: "0.8rem", color: "var(--ink-faint)", margin: "4px 0 0" }}>
                  {merchantStatus.operationState}
                </p>
              )}
            </div>
          </div>
          <Link to="/integracoes/ifood/status" style={{ textDecoration: "none" }}>
            <Button variant="ghost">Ver detalhes →</Button>
          </Link>
        </div>
      )}

      {errorMessage && (
        <QueryError error={errorMessage} what="as informações do dashboard" />
      )}

      {!isLoading && !isError && (
        <>
          {/* Seção 1: Métricas Principais */}
          <div style={{ marginBottom: 32 }}>
            <h3 style={{ fontSize: "1rem", fontWeight: 700, marginBottom: 12 }}>Resumo de Pedidos</h3>
            <StatsGrid columns={4}>
              <DashboardCard
                title="Total de Pedidos"
                value={metrics.total}
                icon="📦"
                status="info"
                subtitle="Pedidos sincronizados"
              />
              <DashboardCard title="Entregues" value={metrics.delivered} status="success" icon="✓" />
              <DashboardCard
                title="Em Progresso"
                value={metrics.inProgress}
                status="warning"
                icon="⏳"
              />
              <DashboardCard
                title="Cancelados"
                value={metrics.cancelled}
                status="error"
                icon="✕"
              />
            </StatsGrid>
          </div>

          {/* Seção 2: Breakdown por Tipo de Operação */}
          <div style={{ marginBottom: 32, display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(280px, 1fr))", gap: 12 }}>
            <div className="card" style={{ padding: 16 }}>
              <h4 style={{ fontSize: "0.9rem", fontWeight: 600, margin: "0 0 12px" }}>
                📊 Pedidos por Tipo
              </h4>
              <MetricsRow
                metric="Delivery"
                value={metrics.deliveryOrders}
                icon="🚗"
              />
              <MetricsRow
                metric="Retirada"
                value={metrics.takeoutOrders}
                icon="🛍️"
              />
              <MetricsRow
                metric="Consumo no Local"
                value={metrics.dineInOrders}
                icon="🍽️"
              />
            </div>

            {/* Financeiro */}
            {financial && (
              <div className="card" style={{ padding: 16 }}>
                <h4 style={{ fontSize: "0.9rem", fontWeight: 600, margin: "0 0 12px" }}>
                  💰 Financeiro
                </h4>
                <MetricsRow
                  metric="Eventos com repasse"
                  value={formatCurrency(financial.totalFinancialEventsWithTransferImpact)}
                />
                <MetricsRow metric="Repasses recebidos" value={formatCurrency(financial.totalSettlements)} />
                <MetricsRow
                  metric="Divergência"
                  value={financial.hasDiscrepancy ? formatCurrency(financial.discrepancyAmount) : "—"}
                />
              </div>
            )}

            {/* Reviews */}
            <div className="card" style={{ padding: 16 }}>
              <h4 style={{ fontSize: "0.9rem", fontWeight: 600, margin: "0 0 12px" }}>
                ⭐ Avaliações
              </h4>
              <MetricsRow
                metric="Rating Médio"
                value={avgRating}
                icon="⭐"
              />
              <MetricsRow
                metric="Total de Reviews"
                value={reviews.length}
              />
              <MetricsRow
                metric="Respondidas"
                value={respondedReviews}
                unit={`de ${reviews.length}`}
              />
            </div>
          </div>

          {/* Seção 3: Pedidos Recentes */}
          {orders.length > 0 && (
            <div style={{ marginBottom: 32 }}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 12 }}>
                <h3 style={{ fontSize: "1rem", fontWeight: 700, margin: 0 }}>
                  Pedidos Abertos
                </h3>
                <Link to="/integracoes/ifood/pedidos" style={{ textDecoration: "none" }}>
                  <Button variant="ghost">Ver todos →</Button>
                </Link>
              </div>

              <div className="card" style={{ padding: 0, overflow: "hidden" }}>
                <table style={{ width: "100%", borderCollapse: "collapse" }}>
                  <thead>
                    <tr style={{ borderBottom: "1px solid var(--border)", background: "var(--surface-2)" }}>
                      <th style={{ padding: 12, textAlign: "left", fontSize: "0.85rem", fontWeight: 600 }}>
                        Pedido
                      </th>
                      <th style={{ padding: 12, textAlign: "left", fontSize: "0.85rem", fontWeight: 600 }}>
                        Status
                      </th>
                      <th style={{ padding: 12, textAlign: "left", fontSize: "0.85rem", fontWeight: 600 }}>
                        Tipo
                      </th>
                      <th style={{ padding: 12, textAlign: "right", fontSize: "0.85rem", fontWeight: 600 }}>
                        Valor
                      </th>
                      <th style={{ padding: 12, textAlign: "left", fontSize: "0.85rem", fontWeight: 600 }}>
                        Horário
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {orders.slice(0, 5).map((order) => {
                      const statusInfo = formatOrderStatus(order.status);
                      return (
                        <tr
                          key={order.id}
                          style={{ borderBottom: "1px solid var(--border)" }}
                        >
                          <td style={{ padding: 12, fontSize: "0.9rem", fontWeight: 600 }}>
                            {order.displayId || order.ifoodOrderId}
                          </td>
                          <td style={{ padding: 12 }}>
                            <span
                              style={{
                                fontSize: "0.8rem",
                                padding: "4px 8px",
                                borderRadius: 4,
                                background: statusInfo.color + "20",
                                color: statusInfo.color,
                                fontWeight: 600,
                              }}
                            >
                              {statusInfo.icon} {statusInfo.label}
                            </span>
                          </td>
                          <td style={{ padding: 12, fontSize: "0.9rem" }}>
                            {ORDER_TYPE_LABELS[order.ifoodOrderType] ?? "🍽️ Local"}
                          </td>
                          <td style={{ padding: 12, textAlign: "right", fontSize: "0.9rem", fontWeight: 600 }}>
                            {formatCurrency(order.totalAmount)}
                          </td>
                          <td style={{ padding: 12, fontSize: "0.85rem", color: "var(--ink-faint)" }}>
                            {formatTime(order.createdAt)}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* Seção 4: Links Rápidos */}
          <div style={{ marginBottom: 32 }}>
            <h3 style={{ fontSize: "1rem", fontWeight: 700, marginBottom: 12 }}>Acesso Rápido</h3>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))", gap: 12 }}>
              <Link to="/integracoes/ifood/status" style={{ textDecoration: "none" }}>
                <Button variant="ghost" style={{ width: "100%", justifyContent: "flex-start" }}>
                  🔍 Status & Validações
                </Button>
              </Link>
              <Link to="/integracoes/ifood/pedidos" style={{ textDecoration: "none" }}>
                <Button variant="ghost" style={{ width: "100%", justifyContent: "flex-start" }}>
                  📦 Gerenciar Pedidos
                </Button>
              </Link>
              <Link to="/integracoes/ifood/financeiro/relatorios" style={{ textDecoration: "none" }}>
                <Button variant="ghost" style={{ width: "100%", justifyContent: "flex-start" }}>
                  💰 Financeiro Detalhado
                </Button>
              </Link>
              <Link to="/integracoes/ifood/avaliacoes" style={{ textDecoration: "none" }}>
                <Button variant="ghost" style={{ width: "100%", justifyContent: "flex-start" }}>
                  ⭐ Respostas de Avaliações
                </Button>
              </Link>
              <Link to="/integracoes/ifood/logistica" style={{ textDecoration: "none" }}>
                <Button variant="ghost" style={{ width: "100%", justifyContent: "flex-start" }}>
                  🚗 Status de Entregas
                </Button>
              </Link>
              <Link to="/integracoes/ifood/indicadores" style={{ textDecoration: "none" }}>
                <Button variant="ghost" style={{ width: "100%", justifyContent: "flex-start" }}>
                  📊 Indicadores Analytics
                </Button>
              </Link>
            </div>
          </div>
        </>
      )}
    </main>
  );
}
