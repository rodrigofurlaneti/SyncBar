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
import { QueryError } from "../../components/QueryError";
import { EmptyState } from "../../ui/EmptyState";

// Fase 9 — Avaliações: review/v1.0. Sem persistência local, sempre lido/escrito direto no
// iFood (ao contrário de Financial/Shipping, que guardam uma cópia local pra dedup/histórico).
const PAGE_SIZE = 10;

function formatDate(value: string | null): string {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString("pt-BR");
}

export function IFoodReviewsPage() {
  const queryClient = useQueryClient();
  const toast = useToast();
  const { branchId } = useAuthStore();
  const [page, setPage] = useState(1);
  const [replying, setReplying] = useState<IFoodReviewListItem | null>(null);
  const [replyText, setReplyText] = useState("");
  const [viewingId, setViewingId] = useState<string | null>(null);

  const summaryQuery = useQuery({
    queryKey: ["integrations", "ifood", "reviews", "summary", branchId],
    queryFn: () => getIFoodReviewsSummary(branchId),
  });

  const listQuery = useQuery({
    queryKey: ["integrations", "ifood", "reviews", branchId, page],
    queryFn: () => getIFoodReviews(branchId, { page, pageSize: PAGE_SIZE }),
  });

  const detailQuery = useQuery({
    queryKey: ["integrations", "ifood", "reviews", "detail", branchId, viewingId],
    queryFn: () => getIFoodReviewById(branchId, viewingId!),
    enabled: !!viewingId,
  });

  const replyMutation = useMutation({
    mutationFn: () => replyIFoodReview(branchId, replying!.id, replyText),
    onSuccess: () => {
      toast.success("Resposta enviada ao iFood.");
      setReplying(null);
      setReplyText("");
      void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "reviews"] });
    },
    onError: (error: unknown) => toast.error(error instanceof Error ? error.message : "Falha ao responder a avaliação."),
  });

  const reviews = listQuery.data?.reviews ?? [];
  const pageCount = listQuery.data?.pageCount ?? 0;

  return (
    <main style={{ padding: 22, maxWidth: 1000, margin: "0 auto" }}>
      <div className="rise" style={{ marginBottom: 18 }}>
        <Link to="/integracoes/ifood" style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
          ← Integração iFood
        </Link>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>
          Avaliações iFood
        </h2>
        <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
          veja e responda às avaliações dos clientes direto no iFood
        </span>
      </div>

      {summaryQuery.data && (
        <div className="ui-row ui-row-wrap" style={{ gap: 12, marginBottom: 18 }}>
          <div className="card" style={{ padding: 14, minWidth: 160 }}>
            <div style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>Nota média</div>
            <div style={{ fontSize: "1.6rem", fontWeight: 700 }}>{summaryQuery.data.score?.toFixed(2) ?? "—"}</div>
          </div>
          <div className="card" style={{ padding: 14, minWidth: 160 }}>
            <div style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>Total de avaliações</div>
            <div style={{ fontSize: "1.6rem", fontWeight: 700 }}>{summaryQuery.data.totalReviewsCount}</div>
          </div>
          <div className="card" style={{ padding: 14, minWidth: 160 }}>
            <div style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>Avaliações válidas</div>
            <div style={{ fontSize: "1.6rem", fontWeight: 700 }}>{summaryQuery.data.validReviewsCount}</div>
          </div>
        </div>
      )}

      {listQuery.isError && <QueryError error={listQuery.error} what="as avaliações do iFood" />}

      {!listQuery.isLoading && reviews.length === 0 && !listQuery.isError && (
        <EmptyState title="Nenhuma avaliação" description="Ainda não há avaliações de clientes pra esta filial." />
      )}

      <div style={{ display: "grid", gap: 10 }}>
        {reviews.map((review) => (
          <div key={review.id} className="card" style={{ padding: 14, display: "grid", gap: 6 }}>
            <div className="ui-row" style={{ justifyContent: "space-between" }}>
              <strong>{review.score !== null ? `⭐ ${review.score.toFixed(1)}` : "Sem nota"}</strong>
              <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>{formatDate(review.createdAt)}</span>
            </div>
            <p style={{ margin: 0 }}>{review.comment || <em>sem comentário</em>}</p>
            {review.reply && (
              <div style={{ background: "var(--surface-2)", borderRadius: 8, padding: 8, fontSize: "0.85rem" }}>
                <strong>Sua resposta:</strong> {review.reply}
              </div>
            )}
            <div className="ui-row" style={{ gap: 8 }}>
              <Button variant="ghost" onClick={() => setViewingId(review.id)}>
                Ver detalhes
              </Button>
              {!review.reply && (
                <Button
                  variant="primary"
                  onClick={() => {
                    setReplying(review);
                    setReplyText("");
                  }}
                >
                  Responder
                </Button>
              )}
            </div>
          </div>
        ))}
      </div>

      {pageCount > 1 && (
        <div className="ui-row" style={{ gap: 8, marginTop: 16, justifyContent: "center" }}>
          <Button variant="ghost" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>
            ← Anterior
          </Button>
          <span style={{ alignSelf: "center", fontSize: "0.85rem", color: "var(--ink-faint)" }}>
            página {page} de {pageCount}
          </span>
          <Button variant="ghost" disabled={page >= pageCount} onClick={() => setPage((p) => p + 1)}>
            Próxima →
          </Button>
        </div>
      )}

      {replying && (
        <Modal title="Responder avaliação" onClose={() => setReplying(null)}>
          <div style={{ display: "grid", gap: 12 }}>
            <p style={{ margin: 0, color: "var(--ink-faint)" }}>{replying.comment}</p>
            <Field label="Sua resposta">
              {(a11y) => (
                <textarea
                  {...a11y}
                  value={replyText}
                  onChange={(e) => setReplyText(e.target.value)}
                  rows={4}
                  style={{ width: "100%", resize: "vertical", font: "inherit" }}
                />
              )}
            </Field>
            <div className="ui-row" style={{ justifyContent: "flex-end", gap: 8 }}>
              <Button variant="ghost" onClick={() => setReplying(null)}>
                Cancelar
              </Button>
              <Button
                variant="primary"
                disabled={!replyText.trim() || replyMutation.isPending}
                onClick={() => replyMutation.mutate()}
              >
                Enviar resposta
              </Button>
            </div>
          </div>
        </Modal>
      )}

      {viewingId && (
        <Modal title="Detalhes da avaliação" onClose={() => setViewingId(null)}>
          {detailQuery.isLoading && <p>Carregando…</p>}
          {detailQuery.data && (
            <div style={{ display: "grid", gap: 10 }}>
              <div>
                <strong>Cliente:</strong> {detailQuery.data.customerName ?? "—"}
              </div>
              <div>
                <strong>Nota:</strong> {detailQuery.data.score ?? "—"}
              </div>
              <div>
                <strong>Comentário:</strong> {detailQuery.data.comment ?? "—"}
              </div>
              {detailQuery.data.questions.length > 0 && (
                <div>
                  <strong>Pesquisa:</strong>
                  <ul style={{ margin: "4px 0 0 18px" }}>
                    {detailQuery.data.questions.map((q) => (
                      <li key={q.id}>
                        {q.title}: {q.answers.map((a) => a.title).join(", ")}
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          )}
        </Modal>
      )}
    </main>
  );
}
