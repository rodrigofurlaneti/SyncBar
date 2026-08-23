import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  getIFoodReviews,
  respondIFoodReview,
  type IFoodReviewResponse,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { useToast } from "../../ui/Toast";
import { Button } from "../../ui/Button";
import { Modal } from "../../ui/Modal";
import { TextField, SelectField } from "../../ui/Field";
import { PageHeader } from "../../components/PageHeader";
import { QueryError } from "../../components/QueryError";
import { EmptyState } from "../../ui/EmptyState";
import { Tabs } from "../../components/Tabs";
import { DashboardCard } from "../../components/DashboardCard";
import { formatReviewState, formatDate, formatDateTimeShort, formatCurrency } from "../../utils/ifoodFormattersEnhanced";

export function IFoodReviewsDetailedPage() {
  const { branchId } = useAuthStore();
  const navigate = useNavigate();
  const toast = useToast();
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState("aberta");
  const [respondingReview, setRespondingReview] = useState<IFoodReviewResponse | null>(null);
  const [response, setResponse] = useState("");

  const reviewsQuery = useQuery({
    queryKey: ["integrations", "ifood", "reviews", branchId],
    queryFn: () => getIFoodReviews(branchId, { limit: 100 }),
    refetchInterval: 60000,
  });

  const respondMutation = useMutation({
    mutationFn: (payload: { reviewId: string; response: string }) =>
      respondIFoodReview(branchId, payload.reviewId, payload.response),
    onSuccess: () => {
      toast.success("Resposta enviada com sucesso!");
      setRespondingReview(null);
      setResponse("");
      void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "reviews"] });
    },
    onError: () => toast.error("Falha ao enviar resposta."),
  });

  const reviews = reviewsQuery.data || [];
  const openReviews = reviews.filter((r) => r.responseState === "OPEN");
  const respondedReviews = reviews.filter((r) => r.responseState === "CLOSED");
  const avgRating = reviews.length > 0 ? (reviews.reduce((sum, r) => sum + r.rating, 0) / reviews.length).toFixed(1) : 0;

  const displayReviews = activeTab === "aberta" ? openReviews : respondedReviews;

  if (reviewsQuery.isLoading) {
    return (
      <main style={{ padding: 22, maxWidth: 1000, margin: "0 auto" }}>
        <PageHeader
          title="Avaliações iFood"
          subtitle="Gerencie respostas a clientes"
          breadcrumb={[{ label: "iFood", href: "/integracoes/ifood" }]}
        />
        <div style={{ textAlign: "center", padding: "40px 20px" }}>
          <p style={{ color: "var(--ink-faint)" }}>Carregando avaliações...</p>
        </div>
      </main>
    );
  }

  if (reviewsQuery.isError) {
    return (
      <main style={{ padding: 22, maxWidth: 1000, margin: "0 auto" }}>
        <PageHeader
          title="Avaliações iFood"
          subtitle="Gerencie respostas a clientes"
          breadcrumb={[{ label: "iFood", href: "/integracoes/ifood" }]}
        />
        <QueryError error={reviewsQuery.error} what="as avaliações" />
      </main>
    );
  }

  return (
    <main style={{ padding: 22, maxWidth: 1000, margin: "0 auto" }}>
      <PageHeader
        title="Avaliações iFood"
        subtitle="Gerencie respostas a clientes"
        breadcrumb={[{ label: "iFood", href: "/integracoes/ifood" }]}
        actions={
          <Button
            variant="ghost"
            onClick={() => reviewsQuery.refetch()}
            disabled={reviewsQuery.isRefetching}
          >
            🔄 {reviewsQuery.isRefetching ? "Atualizando..." : "Atualizar agora"}
          </Button>
        }
      />

      {/* Resumo de Métricas */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))",
          gap: 12,
          marginBottom: 24,
        }}
      >
        <DashboardCard
          title="Rating Médio"
          value={`${avgRating} ⭐`}
          subtitle={`de ${reviews.length} avaliações`}
          status="info"
          icon="📊"
        />
        <DashboardCard
          title="Não Respondidas"
          value={openReviews.length}
          status={openReviews.length > 0 ? "warning" : "success"}
          icon="📝"
        />
        <DashboardCard
          title="Respondidas"
          value={respondedReviews.length}
          status="success"
          icon="✓"
        />
        <DashboardCard
          title="Taxa de Resposta"
          value={`${((respondedReviews.length / Math.max(reviews.length, 1)) * 100).toFixed(0)}%`}
          status="info"
          icon="📈"
        />
      </div>

      {/* Abas */}
      <Tabs
        tabs={[
          { id: "aberta", label: "Não Respondidas", badge: openReviews.length },
          { id: "respondida", label: "Respondidas", badge: respondedReviews.length },
        ]}
        activeTab={activeTab}
        onTabChange={setActiveTab}
      >
        {displayReviews.length === 0 ? (
          <div style={{ marginTop: 20 }}>
            <EmptyState
              title={activeTab === "aberta" ? "Todas respondidas!" : "Nenhuma respondida"}
              description={
                activeTab === "aberta"
                  ? "Parabéns! Você respondeu todas as avaliações."
                  : "Comece respondendo avaliações dos clientes."
              }
            />
          </div>
        ) : (
          <div style={{ display: "grid", gap: 16, marginTop: 16 }}>
            {displayReviews.map((review) => {
              const stateDisplay = formatReviewState(review.responseState);
              return (
                <div
                  key={review.id}
                  className="card"
                  style={{
                    padding: 16,
                    borderLeft: `4px solid ${stateDisplay.color}`,
                  }}
                >
                  <div
                    style={{
                      display: "grid",
                      gridTemplateColumns: "1fr auto",
                      alignItems: "flex-start",
                      gap: 12,
                      marginBottom: 12,
                    }}
                  >
                    <div>
                      <div style={{ display: "flex", gap: 8, alignItems: "center", marginBottom: 4 }}>
                        <span style={{ fontSize: "1.2rem" }}>{"⭐".repeat(Math.floor(review.rating))}</span>
                        <span style={{ fontSize: "0.9rem", color: "var(--ink-faint)" }}>
                          {review.rating.toFixed(1)}
                        </span>
                      </div>
                      <p style={{ fontSize: "0.9rem", fontWeight: 600, margin: 0, marginBottom: 4 }}>
                        {review.customerName}
                      </p>
                      <p style={{ fontSize: "0.8rem", color: "var(--ink-faint)", margin: 0 }}>
                        {formatDateTimeShort(review.createdAt)}
                      </p>
                    </div>
                    <span
                      style={{
                        fontSize: "0.8rem",
                        padding: "4px 8px",
                        borderRadius: 4,
                        background: stateDisplay.color + "20",
                        color: stateDisplay.color,
                        fontWeight: 600,
                      }}
                    >
                      {stateDisplay.icon} {stateDisplay.label}
                    </span>
                  </div>

                  {/* Avaliação */}
                  <div
                    style={{
                      padding: 12,
                      borderRadius: 6,
                      background: "var(--surface-2)",
                      marginBottom: review.response ? 12 : 0,
                      borderLeft: "3px solid var(--accent)",
                    }}
                  >
                    <p style={{ fontSize: "0.9rem", margin: 0, lineHeight: 1.5 }}>
                      {review.message}
                    </p>
                  </div>

                  {/* Resposta (se houver) */}
                  {review.response && (
                    <div
                      style={{
                        padding: 12,
                        borderRadius: 6,
                        background: "#e8f5e9",
                        borderLeft: "3px solid #4caf50",
                        marginBottom: 12,
                      }}
                    >
                      <p
                        style={{
                          fontSize: "0.75rem",
                          fontWeight: 600,
                          color: "#1b5e20",
                          margin: "0 0 4px",
                        }}
                      >
                        ✓ Resposta da loja:
                      </p>
                      <p style={{ fontSize: "0.9rem", margin: 0, lineHeight: 1.5 }}>
                        {review.response}
                      </p>
                    </div>
                  )}

                  {/* Ações */}
                  {review.responseState === "OPEN" && (
                    <Button
                      variant="primary"
                      onClick={() => setRespondingReview(review)}
                      style={{ width: "100%" }}
                    >
                      ✉️ Responder
                    </Button>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </Tabs>

      {/* Modal de Resposta */}
      {respondingReview && (
        <Modal
          title="Responder Avaliação"
          onClose={() => {
            setRespondingReview(null);
            setResponse("");
          }}
          footer={
            <>
              <Button
                variant="ghost"
                onClick={() => {
                  setRespondingReview(null);
                  setResponse("");
                }}
              >
                Cancelar
              </Button>
              <Button
                variant="primary"
                onClick={() =>
                  respondMutation.mutate({
                    reviewId: respondingReview.id,
                    response,
                  })
                }
                disabled={!response.trim() || respondMutation.isPending}
              >
                {respondMutation.isPending ? "Enviando..." : "Enviar Resposta"}
              </Button>
            </>
          }
        >
          <div style={{ display: "grid", gap: 12 }}>
            <div>
              <p style={{ fontSize: "0.9rem", fontWeight: 600, margin: "0 0 8px" }}>
                Avaliação de {respondingReview.customerName}
              </p>
              <div
                style={{
                  padding: 12,
                  background: "var(--surface-2)",
                  borderRadius: 6,
                  borderLeft: "3px solid var(--accent)",
                }}
              >
                <div style={{ fontSize: "1rem", marginBottom: 4 }}>
                  {"⭐".repeat(Math.floor(respondingReview.rating))}
                </div>
                <p style={{ fontSize: "0.9rem", margin: 0, lineHeight: 1.5 }}>
                  {respondingReview.message}
                </p>
              </div>
            </div>

            <TextField
              label="Sua Resposta"
              value={response}
              onChange={(e) => setResponse(e.target.value)}
              placeholder="Digite sua resposta ao cliente..."
              multiline
              rows={4}
            />

            <p style={{ fontSize: "0.75rem", color: "var(--ink-faint)", margin: 0 }}>
              Máximo 500 caracteres. Seja educado e profissional.
            </p>
          </div>
        </Modal>
      )}

      {/* Footer */}
      <div
        style={{
          marginTop: 32,
          padding: 16,
          borderRadius: 8,
          background: "var(--surface-2)",
        }}
      >
        <Link to="/integracoes/ifood" style={{ textDecoration: "none" }}>
          <Button variant="ghost">← Voltar ao iFood</Button>
        </Link>
      </div>
    </main>
  );
}
