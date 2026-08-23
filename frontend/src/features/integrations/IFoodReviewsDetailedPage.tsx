import { useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  getIFoodReviewById,
  getIFoodReviews,
  getIFoodReviewsSummary,
  replyIFoodReview,
  type IFoodReviewListItem,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { useToast } from "../../ui/Toast";
import { Button } from "../../ui/Button";
import { Modal } from "../../ui/Modal";
import { Field } from "../../ui/Field";
import { PageHeader } from "../../components/PageHeader";
import { QueryError } from "../../components/QueryError";
import { EmptyState } from "../../ui/EmptyState";
import { Tabs } from "../../components/Tabs";
import { DashboardCard } from "../../components/DashboardCard";
import { formatReviewState, formatDateTimeShort } from "../../utils/ifoodFormattersEnhanced";

const PAGE_SIZE = 20;
// Limite do texto de resposta aceito pelo iFood (módulo Review v1.0).
const MAX_REPLY_LENGTH = 500;

export function IFoodReviewsDetailedPage() {
  const { branchId } = useAuthStore();
  const toast = useToast();
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState("aberta");
  const [page, setPage] = useState(1);
  const [replyingReview, setReplyingReview] = useState<IFoodReviewListItem | null>(null);
  const [replyText, setReplyText] = useState("");
  const [viewingId, setViewingId] = useState<string | null>(null);

  const summaryQuery = useQuery({
    queryKey: ["integrations", "ifood", "reviews", "summary", branchId],
    queryFn: () => getIFoodReviewsSummary(branchId),
  });

  const reviewsQuery = useQuery({
    queryKey: ["integrations", "ifood", "reviews", branchId, page],
    queryFn: () => getIFoodReviews(branchId, { page, pageSize: PAGE_SIZE }),
    refetchInterval: 60000,
  });

  const detailQuery = useQuery({
    queryKey: ["integrations", "ifood", "reviews", "detail", branchId, viewingId],
    queryFn: () => getIFoodReviewById(branchId, viewingId!),
    enabled: !!viewingId,
  });

  const replyMutation = useMutation({
    mutationFn: (payload: { reviewId: string; text: string }) =>
      replyIFoodReview(branchId, payload.reviewId, payload.text),
    onSuccess: () => {
      toast.success("Resposta enviada ao iFood.");
      setReplyingReview(null);
      setReplyText("");
      void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "reviews"] });
    },
    onError: () => toast.error("Falha ao enviar resposta."),
  });

  const reviews = reviewsQuery.data?.reviews ?? [];
  // A lista do iFood não tem um estado de resposta: "respondida" é a avaliação que já tem
  // `reply` preenchido.
  const openReviews = reviews.filter((r) => !r.reply);
  const repliedReviews = reviews.filter((r) => !!r.reply);
  const displayReviews = activeTab === "aberta" ? openReviews : repliedReviews;

  const summary = summaryQuery.data;
  const avgScore = summary?.score != null ? summary.score.toFixed(1) : "—";
  const totalReviews = summary?.totalReviewsCount ?? reviewsQuery.data?.total ?? reviews.length;
  const pageCount = reviewsQuery.data?.pageCount ?? 1;

  const replyTooLong = replyText.length > MAX_REPLY_LENGTH;

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
          <Button variant="ghost" onClick={() => reviewsQuery.refetch()} disabled={reviewsQuery.isRefetching}>
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
          title="Nota Média"
          value={`${avgScore} ⭐`}
          subtitle={`de ${totalReviews} avaliações`}
          status="info"
          icon="📊"
        />
        <DashboardCard
          title="Não Respondidas (página)"
          value={openReviews.length}
          status={openReviews.length > 0 ? "warning" : "success"}
          icon="📝"
        />
        <DashboardCard title="Respondidas (página)" value={repliedReviews.length} status="success" icon="✓" />
        <DashboardCard
          title="Taxa de Resposta (página)"
          value={`${((repliedReviews.length / Math.max(reviews.length, 1)) * 100).toFixed(0)}%`}
          status="info"
          icon="📈"
        />
      </div>

      {/* Abas */}
      <Tabs
        tabs={[
          { id: "aberta", label: "Não Respondidas", badge: openReviews.length },
          { id: "respondida", label: "Respondidas", badge: repliedReviews.length },
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
                  ? "Nenhuma avaliação pendente de resposta nesta página."
                  : "Comece respondendo avaliações dos clientes."
              }
            />
          </div>
        ) : (
          <div style={{ display: "grid", gap: 16, marginTop: 16 }}>
            {displayReviews.map((review) => {
              const stateDisplay = formatReviewState(review.reply ? "CLOSED" : "OPEN");
              return (
                <div
                  key={review.id}
                  className="card"
                  style={{ padding: 16, borderLeft: `4px solid ${stateDisplay.color}` }}
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
                        <span style={{ fontSize: "1.2rem" }}>
                          {review.score != null ? "⭐".repeat(Math.round(review.score)) : "—"}
                        </span>
                        {review.score != null && (
                          <span style={{ fontSize: "0.9rem", color: "var(--ink-faint)" }}>
                            {review.score.toFixed(1)}
                          </span>
                        )}
                      </div>
                      <p style={{ fontSize: "0.9rem", fontWeight: 600, margin: 0, marginBottom: 4 }}>
                        Pedido {review.order?.shortId ?? review.order?.id ?? "—"}
                      </p>
                      <p style={{ fontSize: "0.8rem", color: "var(--ink-faint)", margin: 0 }}>
                        {review.createdAt ? formatDateTimeShort(review.createdAt) : "—"}
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

                  {/* Comentário do cliente */}
                  <div
                    style={{
                      padding: 12,
                      borderRadius: 6,
                      background: "var(--surface-2)",
                      marginBottom: 12,
                      borderLeft: "3px solid var(--accent)",
                    }}
                  >
                    <p style={{ fontSize: "0.9rem", margin: 0, lineHeight: 1.5 }}>
                      {review.comment || "Avaliação sem comentário."}
                    </p>
                  </div>

                  {/* Resposta (se houver) */}
                  {review.reply && (
                    <div
                      style={{
                        padding: 12,
                        borderRadius: 6,
                        background: "#e8f5e9",
                        borderLeft: "3px solid #4caf50",
                        marginBottom: 12,
                      }}
                    >
                      <p style={{ fontSize: "0.75rem", fontWeight: 600, color: "#1b5e20", margin: "0 0 4px" }}>
                        ✓ Resposta da loja:
                      </p>
                      <p style={{ fontSize: "0.9rem", margin: 0, lineHeight: 1.5 }}>{review.reply}</p>
                    </div>
                  )}

                  {/* Ações */}
                  <div style={{ display: "flex", gap: 8 }}>
                    <Button variant="ghost" onClick={() => setViewingId(review.id)}>
                      🔍 Ver detalhes
                    </Button>
                    {!review.reply && (
                      <Button variant="primary" onClick={() => setReplyingReview(review)} style={{ flex: 1 }}>
                        ✉️ Responder
                      </Button>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </Tabs>

      {/* Paginação */}
      <div style={{ display: "flex", gap: 8, alignItems: "center", justifyContent: "center", marginTop: 20 }}>
        <Button variant="ghost" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>
          ← Anterior
        </Button>
        <span style={{ fontSize: "0.85rem", color: "var(--ink-faint)" }}>
          Página {page} de {Math.max(pageCount, 1)}
        </span>
        <Button variant="ghost" disabled={page >= pageCount} onClick={() => setPage((p) => p + 1)}>
          Próxima →
        </Button>
      </div>

      {/* Modal de Resposta */}
      {replyingReview && (
        <Modal
          title="Responder Avaliação"
          onClose={() => {
            setReplyingReview(null);
            setReplyText("");
          }}
        >
          <div style={{ display: "grid", gap: 12 }}>
            <div>
              <p style={{ fontSize: "0.9rem", fontWeight: 600, margin: "0 0 8px" }}>
                Avaliação do pedido {replyingReview.order?.shortId ?? replyingReview.order?.id ?? "—"}
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
                  {replyingReview.score != null ? "⭐".repeat(Math.round(replyingReview.score)) : "—"}
                </div>
                <p style={{ fontSize: "0.9rem", margin: 0, lineHeight: 1.5 }}>
                  {replyingReview.comment || "Avaliação sem comentário."}
                </p>
              </div>
            </div>

            <Field
              label="Sua Resposta"
              hint={`${replyText.length}/${MAX_REPLY_LENGTH} caracteres. Seja educado e profissional.`}
              error={replyTooLong ? `A resposta passa de ${MAX_REPLY_LENGTH} caracteres.` : undefined}
            >
              {(a11y) => (
                <textarea
                  {...a11y}
                  rows={4}
                  maxLength={MAX_REPLY_LENGTH}
                  value={replyText}
                  onChange={(e) => setReplyText(e.target.value)}
                  placeholder="Digite sua resposta ao cliente..."
                />
              )}
            </Field>

            <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
              <Button
                variant="ghost"
                onClick={() => {
                  setReplyingReview(null);
                  setReplyText("");
                }}
              >
                Cancelar
              </Button>
              <Button
                variant="primary"
                onClick={() => replyMutation.mutate({ reviewId: replyingReview.id, text: replyText.trim() })}
                disabled={!replyText.trim() || replyTooLong || replyMutation.isPending}
              >
                {replyMutation.isPending ? "Enviando..." : "Enviar Resposta"}
              </Button>
            </div>
          </div>
        </Modal>
      )}

      {/* Modal de Detalhes */}
      {viewingId && (
        <Modal title="Detalhes da Avaliação" onClose={() => setViewingId(null)}>
          {detailQuery.isLoading && <p style={{ color: "var(--ink-faint)" }}>Carregando...</p>}
          {detailQuery.isError && <QueryError error={detailQuery.error} what="os detalhes da avaliação" />}
          {detailQuery.data && (
            <div style={{ display: "grid", gap: 12 }}>
              <p style={{ margin: 0, fontSize: "0.9rem" }}>
                <strong>Cliente:</strong> {detailQuery.data.customerName || "—"}
              </p>
              <p style={{ margin: 0, fontSize: "0.9rem" }}>
                <strong>Nota:</strong> {detailQuery.data.score != null ? detailQuery.data.score.toFixed(1) : "—"}
              </p>
              <p style={{ margin: 0, fontSize: "0.9rem" }}>
                <strong>Comentário:</strong> {detailQuery.data.comment || "—"}
              </p>
              {detailQuery.data.questions.length > 0 && (
                <div style={{ display: "grid", gap: 8 }}>
                  <strong style={{ fontSize: "0.9rem" }}>Perguntas</strong>
                  {detailQuery.data.questions.map((question) => (
                    <div key={question.id} className="card" style={{ padding: 12 }}>
                      <p style={{ margin: 0, fontSize: "0.9rem", fontWeight: 600 }}>{question.title || question.id}</p>
                      <ul style={{ margin: "4px 0 0", paddingLeft: 18, fontSize: "0.85rem" }}>
                        {question.answers.map((answer) => (
                          <li key={answer.id}>{answer.title || answer.id}</li>
                        ))}
                      </ul>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </Modal>
      )}

      {/* Footer */}
      <div style={{ marginTop: 32, padding: 16, borderRadius: 8, background: "var(--surface-2)" }}>
        <Link to="/integracoes/ifood" style={{ textDecoration: "none" }}>
          <Button variant="ghost">← Voltar ao iFood</Button>
        </Link>
      </div>
    </main>
  );
}
