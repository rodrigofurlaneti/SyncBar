import { useEffect, useState } from "react";
import type { CSSProperties } from "react";
import { useSearchParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  acceptKeetaOrderRefund,
  confirmKeetaOrder,
  createKeetaSetting,
  deleteKeetaSetting,
  dispatchKeetaOrder,
  getActiveKeetaOrdersByBranch,
  getAllKeetaMerchantMappingsByCompany,
  getKeetaAuthorizationUrl,
  keetaCancellationReasonLabel,
  keetaOrderStatusLabel,
  markKeetaOrderDelivered,
  markKeetaOrderReadyForPickup,
  rejectKeetaOrderRefund,
  requestKeetaOrderCancellation,
  resolveKeetaSetting,
  updateKeetaSetting,
  type KeetaIntegrationOrderResponse,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { formatBRL, parseApiDate } from "../../lib/types";
import { ApiError } from "../../lib/apiClient";
import { useToast } from "../../ui/Toast";
import { useDialog } from "../../ui/Dialog";
import { QueryError } from "../../components/QueryError";
import { Button } from "../../ui/Button";
import { SelectField, TextField } from "../../ui/Field";
import { Modal } from "../../ui/Modal";
import { EmptyState } from "../../ui/EmptyState";
import { SkeletonList } from "../../ui/Skeleton";

// Central da integração Keeta (Open Delivery): Configurações (credenciais OAuth
// client_credentials + autorização de lojas) e Pedidos (ciclo de vida — confirmar, despachar,
// entregar, cancelar, reembolsar) — os controllers do backend (KeetaSettingController,
// KeetaOAuthController, KeetaMerchantMappingController, KeetaOrderController) reunidos numa
// única tela em abas, no mesmo padrão visual das demais integrações do app.

type TabId = "settings" | "orders";

const TABS: Array<{ id: TabId; label: string; icon: string }> = [
  { id: "settings", label: "Configurações", icon: "🔑" },
  { id: "orders", label: "Pedidos", icon: "🛵" },
];

function apiErrorMessage(e: unknown, fallback: string): string {
  return e instanceof ApiError ? e.message : fallback;
}

export function KeetaPage() {
  const { companyId: rawCompanyId, branchId } = useAuthStore();
  const companyId = rawCompanyId ?? 1;
  const [tab, setTab] = useState<TabId>("settings");
  const toast = useToast();
  const [searchParams, setSearchParams] = useSearchParams();

  useEffect(() => {
    const authResult = searchParams.get("keetaAuth");
    if (!authResult) return;

    if (authResult === "success") {
      toast.success("Loja autorizada na Keeta com sucesso.");
    } else {
      toast.error("Não foi possível concluir a autorização na Keeta — tente novamente.");
    }

    const next = new URLSearchParams(searchParams);
    next.delete("keetaAuth");
    setSearchParams(next, { replace: true });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchParams]);

  return (
    <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }}>
      <div className="rise" style={{ marginBottom: 18 }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>
          Integração Keeta
        </h2>
        <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
          Credenciais OAuth, autorização de lojas e ciclo de vida dos pedidos da plataforma de
          delivery Keeta (Open Delivery) — somente gerente/administrador.
        </span>
      </div>

      <div
        role="tablist"
        aria-label="Áreas da integração Keeta"
        className="ui-row ui-row-wrap"
        style={{ gap: 4, borderBottom: "1px solid var(--line-soft)", marginBottom: 18 }}
      >
        {TABS.map((t) => (
          <button
            key={t.id}
            type="button"
            role="tab"
            id={`keeta-tab-${t.id}`}
            aria-selected={tab === t.id}
            aria-controls={`keeta-panel-${t.id}`}
            onClick={() => setTab(t.id)}
            style={{
              padding: "10px 16px",
              border: "none",
              borderBottom: tab === t.id ? "2px solid var(--amber)" : "2px solid transparent",
              background: "transparent",
              color: tab === t.id ? "var(--ink)" : "var(--ink-faint)",
              fontWeight: tab === t.id ? 700 : 500,
              fontSize: "0.92rem",
              cursor: "pointer",
              display: "flex",
              alignItems: "center",
              gap: 6,
              transition: "color var(--duration-base) var(--ease-standard), border-color var(--duration-base) var(--ease-standard)",
            }}
          >
            <span aria-hidden="true">{t.icon}</span> {t.label}
          </button>
        ))}
      </div>

      <div id={`keeta-panel-${tab}`} role="tabpanel" aria-labelledby={`keeta-tab-${tab}`} className="rise rise-1">
        {tab === "settings" && <SettingsSection companyId={companyId} branchId={branchId} />}
        {tab === "orders" && <OrdersSection branchId={branchId} />}
      </div>
    </main>
  );
}

// ============================================================================================
// Configurações
// ============================================================================================

function SettingsSection({ companyId, branchId }: { companyId: number; branchId: number }) {
  const queryClient = useQueryClient();
  const toast = useToast();
  const dialog = useDialog();

  const [scope, setScope] = useState<"company" | "branch">("branch");
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [clientId, setClientId] = useState("");
  const [clientSecret, setClientSecret] = useState("");
  const [appId, setAppId] = useState("");
  const [baseUrl, setBaseUrl] = useState("");
  const [authorizing, setAuthorizing] = useState(false);

  const effectiveBranchId = scope === "branch" ? branchId : 0;

  const settingQuery = useQuery({
    queryKey: ["keeta", "settings", "resolve", companyId, effectiveBranchId],
    queryFn: () => resolveKeetaSetting(companyId, effectiveBranchId || undefined),
    retry: false,
  });

  const mappingsQuery = useQuery({
    queryKey: ["keeta", "merchant-mappings", companyId],
    queryFn: () => getAllKeetaMerchantMappingsByCompany(companyId),
  });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ["keeta", "settings"] });
    void queryClient.invalidateQueries({ queryKey: ["keeta", "merchant-mappings"] });
  };

  const resetForm = () => {
    setClientId("");
    setClientSecret("");
    setAppId("");
    setBaseUrl("");
  };

  const createMutation = useMutation({
    mutationFn: () =>
      createKeetaSetting({
        companyId,
        branchId: effectiveBranchId,
        clientId: clientId.trim(),
        clientSecret: clientSecret.trim(),
        appId: appId.trim(),
        baseUrl: baseUrl.trim() === "" ? null : baseUrl.trim(),
      }),
    onSuccess: () => {
      toast.success("Credenciais cadastradas.");
      setCreating(false);
      resetForm();
      refresh();
    },
    onError: (e) => setError(apiErrorMessage(e, "Não foi possível cadastrar as credenciais.")),
  });

  const setting = settingQuery.isSuccess ? settingQuery.data : null;
  const notFound = settingQuery.isError && settingQuery.error instanceof ApiError && settingQuery.error.status === 404;

  const updateMutation = useMutation({
    mutationFn: () =>
      updateKeetaSetting(setting!.id, {
        companyId,
        clientId: clientId.trim() === "" ? undefined : clientId.trim(),
        clientSecret: clientSecret.trim() === "" ? undefined : clientSecret.trim(),
        appId: appId.trim() === "" ? undefined : appId.trim(),
        baseUrl: baseUrl.trim() === "" ? undefined : baseUrl.trim(),
      }),
    onSuccess: () => {
      toast.success("Credenciais atualizadas.");
      setEditing(false);
      resetForm();
      refresh();
    },
    onError: (e) => setError(apiErrorMessage(e, "Não foi possível salvar.")),
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteKeetaSetting(setting!.id, companyId),
    onSuccess: () => {
      toast.success("Configuração removida.");
      refresh();
    },
    onError: (e) => toast.error(apiErrorMessage(e, "Não foi possível remover.")),
  });

  const askDelete = async () => {
    const ok = await dialog.confirm({
      title: "Remover configuração",
      message: "Remover as credenciais Keeta desta empresa/filial? As lojas já autorizadas deixarão de sincronizar até um novo cadastro.",
      danger: true,
      confirmLabel: "Remover",
    });
    if (ok) deleteMutation.mutate();
  };

  const authorize = async () => {
    setAuthorizing(true);
    try {
      // Aponta direto pro endpoint da API (proxied pelo nginx no mesmo domínio em produção —
      // ver frontend/nginx.conf) que efetivamente processa o callback (cria a sessão de
      // autorização e busca os dados da loja); o backend redireciona de volta pra esta tela ao
      // final. companyId/branchId são embutidos pelo próprio backend nesse redirectUri.
      const redirectUri = `${window.location.origin}/api/keeta/oauth/callback`;
      const { merchantAuthorizationUrl } = await getKeetaAuthorizationUrl(companyId, effectiveBranchId, redirectUri);
      window.open(merchantAuthorizationUrl, "_blank", "noopener,noreferrer");
    } catch (e) {
      toast.error(apiErrorMessage(e, "Não foi possível gerar o link de autorização."));
    } finally {
      setAuthorizing(false);
    }
  };

  const scopeLabel = scope === "branch" ? `Filial ${branchId}` : "Padrão da empresa";
  const mappings = mappingsQuery.data ?? [];

  return (
    <section className="ticket rise rise-1" style={{ padding: 20, display: "grid", gap: 14 }}>
      <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", alignItems: "center" }}>
        <div style={{ display: "grid", gap: 4, maxWidth: 620 }}>
          <span className="display" style={{ fontSize: "1.2rem" }}>
            Credenciais OAuth (client_credentials)
          </span>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>
            ClientId/ClientSecret/AppId cadastrados no portal de desenvolvedor da Keeta para esta
            empresa. Depois de salvos, use "Autorizar loja" para gerar o link que o lojista usa
            para conectar sua conta Keeta ao SyncBar.
          </span>
        </div>
        <div style={{ maxWidth: 220 }}>
          <SelectField label="Escopo" value={scope} onChange={(e) => setScope(e.target.value as "company" | "branch")}>
            <option value="branch">Filial atual ({branchId})</option>
            <option value="company">Padrão da empresa</option>
          </SelectField>
        </div>
      </div>

      {settingQuery.isLoading && <SkeletonList rows={1} rowHeight={80} />}
      {settingQuery.isError && !notFound && <QueryError error={settingQuery.error} what="a configuração Keeta" />}

      {notFound && (
        <EmptyState
          icon="🔑"
          title={`Nenhuma credencial cadastrada — ${scopeLabel}`}
          description="Cadastre o ClientId, ClientSecret e AppId fornecidos pela Keeta para começar."
          action={
            <Button variant="primary" onClick={() => { setError(null); resetForm(); setCreating(true); }}>
              + Cadastrar credenciais
            </Button>
          }
        />
      )}

      {setting && (
        <div className="ticket-row" style={{ alignItems: "center" }}>
          <div style={{ display: "grid", gap: 2 }}>
            <span style={{ fontWeight: 600 }}>{scopeLabel}</span>
            <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)", fontFamily: "monospace" }}>
              AppId: {setting.appId ?? "—"} · ClientId: {setting.clientId ?? "—"}
            </span>
            <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
              Atualizado em {parseApiDate(setting.updatedAtUtc).toLocaleString("pt-BR")}
            </span>
          </div>
          <div className="ui-row" style={{ gap: 8, flexWrap: "wrap" }}>
            <span className="chip" style={{ "--dot": setting.hasAccessToken ? "var(--ok)" : "var(--busy)" } as CSSProperties}>
              {setting.hasAccessToken ? "Token ativo" : "Sem token"}
            </span>
            <Button variant="primary" size="sm" loading={authorizing} onClick={() => void authorize()}>
              Autorizar loja
            </Button>
            <Button variant="ghost" size="sm" onClick={() => { setError(null); resetForm(); setEditing(true); }}>
              Editar
            </Button>
            <Button variant="danger" size="sm" loading={deleteMutation.isPending} onClick={() => void askDelete()}>
              Remover
            </Button>
          </div>
        </div>
      )}

      <div style={{ borderTop: "1px solid var(--line-soft)", paddingTop: 14, display: "grid", gap: 10 }}>
        <span className="display" style={{ fontSize: "1.05rem" }}>
          Lojas autorizadas
        </span>

        {mappingsQuery.isError && <QueryError error={mappingsQuery.error} what="as lojas autorizadas" />}
        {mappingsQuery.isLoading && <SkeletonList rows={2} rowHeight={56} />}

        {!mappingsQuery.isLoading && mappings.length === 0 && (
          <EmptyState icon="🏪" title="Nenhuma loja autorizada ainda" description="Use o botão “Autorizar loja” acima para conectar a primeira." />
        )}

        {mappings.length > 0 && (
          <div>
            {mappings.map((m) => (
              <div key={m.id} className="ticket-row" style={{ alignItems: "center" }}>
                <div style={{ display: "grid", gap: 2 }}>
                  <span style={{ fontWeight: 600 }}>{m.storeName}</span>
                  <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>Keeta merchant #{m.keetaMerchantId}</span>
                </div>
                <div className="ui-row" style={{ gap: 8 }}>
                  <span className="chip" style={{ "--dot": m.isAuthorized ? "var(--ok)" : "var(--danger)" } as CSSProperties}>
                    {m.isAuthorized ? "Autorizada" : "Revogada"}
                  </span>
                  <span className="chip" style={{ "--dot": m.isOnboarded ? "var(--ok)" : "var(--busy)" } as CSSProperties}>
                    {m.isOnboarded ? "Onboarding completo" : "Onboarding pendente"}
                  </span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {creating && (
        <Modal title={`Cadastrar credenciais — ${scopeLabel}`} onClose={() => setCreating(false)}>
          <SettingForm
            clientId={clientId} setClientId={setClientId}
            clientSecret={clientSecret} setClientSecret={setClientSecret}
            appId={appId} setAppId={setAppId}
            baseUrl={baseUrl} setBaseUrl={setBaseUrl}
            error={error}
            submitLabel="Cadastrar"
            loading={createMutation.isPending}
            disabled={clientId.trim() === "" || clientSecret.trim() === "" || appId.trim() === ""}
            onSubmit={() => createMutation.mutate()}
          />
        </Modal>
      )}

      {editing && setting && (
        <Modal title={`Editar credenciais — ${scopeLabel}`} onClose={() => setEditing(false)}>
          <SettingForm
            clientId={clientId} setClientId={setClientId}
            clientSecret={clientSecret} setClientSecret={setClientSecret}
            appId={appId} setAppId={setAppId}
            baseUrl={baseUrl} setBaseUrl={setBaseUrl}
            error={error}
            submitLabel="Salvar"
            loading={updateMutation.isPending}
            disabled={false}
            placeholderHint="deixe em branco para manter o valor atual"
            onSubmit={() => updateMutation.mutate()}
          />
        </Modal>
      )}
    </section>
  );
}

function SettingForm({
  clientId, setClientId,
  clientSecret, setClientSecret,
  appId, setAppId,
  baseUrl, setBaseUrl,
  error,
  submitLabel,
  loading,
  disabled,
  placeholderHint,
  onSubmit,
}: {
  clientId: string; setClientId: (v: string) => void;
  clientSecret: string; setClientSecret: (v: string) => void;
  appId: string; setAppId: (v: string) => void;
  baseUrl: string; setBaseUrl: (v: string) => void;
  error: string | null;
  submitLabel: string;
  loading: boolean;
  disabled: boolean;
  placeholderHint?: string;
  onSubmit: () => void;
}) {
  return (
    <div style={{ display: "grid", gap: 12 }}>
      <TextField
        label="Client ID"
        value={clientId}
        onChange={(e) => setClientId(e.target.value)}
        placeholder={placeholderHint ?? "fornecido pelo portal de desenvolvedor da Keeta"}
        autoFocus
      />
      <TextField
        label="Client Secret"
        type="password"
        value={clientSecret}
        onChange={(e) => setClientSecret(e.target.value)}
        placeholder={placeholderHint ?? "usado para gerar o access_token e validar webhooks"}
      />
      <TextField
        label="App ID"
        value={appId}
        onChange={(e) => setAppId(e.target.value)}
        placeholder={placeholderHint ?? "identificador do app cadastrado na Keeta"}
      />
      <TextField
        label="Base URL (opcional)"
        value={baseUrl}
        onChange={(e) => setBaseUrl(e.target.value)}
        placeholder="https://open.mykeeta.com/api/open/opendelivery"
        hint="Deixe em branco para usar o endpoint padrão da API Open Delivery."
      />

      {error && <p className="error-text">{error}</p>}

      <Button variant="primary" block disabled={disabled} loading={loading} onClick={onSubmit}>
        {submitLabel}
      </Button>
    </div>
  );
}

// ============================================================================================
// Pedidos
// ============================================================================================

function orderStatusColor(status: string): string {
  if (status === "CONCLUDED" || status === "DELIVERED") return "var(--ok)";
  if (status.includes("CANCEL") || status === "REFUND_ACCEPTED" || status === "REFUND_REJECTED") return "var(--danger)";
  return "var(--busy)";
}

function OrdersSection({ branchId }: { branchId: number }) {
  const queryClient = useQueryClient();
  const toast = useToast();
  const [cancelling, setCancelling] = useState<KeetaIntegrationOrderResponse | null>(null);
  const [rejecting, setRejecting] = useState<KeetaIntegrationOrderResponse | null>(null);

  const ordersQuery = useQuery({
    queryKey: ["keeta", "orders", "active", branchId],
    queryFn: () => getActiveKeetaOrdersByBranch(branchId),
    refetchInterval: 20000,
  });

  const refresh = () => void queryClient.invalidateQueries({ queryKey: ["keeta", "orders"] });

  const confirmMutation = useMutation({
    mutationFn: (order: KeetaIntegrationOrderResponse) => confirmKeetaOrder(order.id, {}),
    onSuccess: () => { toast.success("Pedido confirmado na Keeta."); refresh(); },
    onError: (e) => toast.error(apiErrorMessage(e, "Não foi possível confirmar o pedido.")),
  });

  const readyMutation = useMutation({
    mutationFn: (order: KeetaIntegrationOrderResponse) => markKeetaOrderReadyForPickup(order.id),
    onSuccess: () => { toast.success("Pedido marcado como pronto para retirada."); refresh(); },
    onError: (e) => toast.error(apiErrorMessage(e, "Não foi possível atualizar o pedido.")),
  });

  const dispatchMutation = useMutation({
    mutationFn: (order: KeetaIntegrationOrderResponse) => dispatchKeetaOrder(order.id, {}),
    onSuccess: () => { toast.success("Pedido despachado."); refresh(); },
    onError: (e) => toast.error(apiErrorMessage(e, "Não foi possível despachar o pedido.")),
  });

  const deliveredMutation = useMutation({
    mutationFn: (order: KeetaIntegrationOrderResponse) => markKeetaOrderDelivered(order.id),
    onSuccess: () => { toast.success("Pedido marcado como entregue."); refresh(); },
    onError: (e) => toast.error(apiErrorMessage(e, "Não foi possível atualizar o pedido.")),
  });

  const acceptRefundMutation = useMutation({
    mutationFn: (order: KeetaIntegrationOrderResponse) => acceptKeetaOrderRefund(order.id),
    onSuccess: () => { toast.success("Reembolso aceito."); refresh(); },
    onError: (e) => toast.error(apiErrorMessage(e, "Não foi possível aceitar o reembolso.")),
  });

  const list = ordersQuery.data ?? [];

  return (
    <section className="ticket rise rise-1" style={{ padding: 20, display: "grid", gap: 14 }}>
      <div style={{ display: "grid", gap: 4, maxWidth: 620 }}>
        <span className="display" style={{ fontSize: "1.2rem" }}>
          Pedidos ativos — Filial {branchId}
        </span>
        <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>
          Pedidos recebidos da Keeta ainda em andamento. As ações abaixo chamam a API real da
          Keeta e atualizam o status local ao mesmo tempo.
        </span>
      </div>

      {ordersQuery.isError && <QueryError error={ordersQuery.error} what="os pedidos" />}
      {ordersQuery.isLoading && <SkeletonList rows={3} rowHeight={84} />}

      {!ordersQuery.isLoading && list.length === 0 && (
        <EmptyState icon="🛵" title="Nenhum pedido ativo" description="Novos pedidos da Keeta aparecem aqui automaticamente." />
      )}

      {list.length > 0 && (
        <div>
          {list.map((o) => (
            <div key={o.id} className="ticket-row" style={{ alignItems: "center" }}>
              <div style={{ display: "grid", gap: 2 }}>
                <span style={{ fontWeight: 600 }}>
                  Pedido {o.displayId} · {formatBRL(o.orderAmount)}
                </span>
                <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                  {o.orderType} · {o.deliveredBy} · recebido em {parseApiDate(o.orderCreatedAtUtc).toLocaleString("pt-BR")}
                </span>
              </div>
              <div className="ui-row ui-row-wrap" style={{ gap: 8, justifyContent: "flex-end" }}>
                <span className="chip" style={{ "--dot": orderStatusColor(o.status) } as CSSProperties}>
                  {keetaOrderStatusLabel[o.status] ?? o.status}
                </span>
                {o.status === "CREATED" && (
                  <Button variant="primary" size="sm" loading={confirmMutation.isPending} onClick={() => confirmMutation.mutate(o)}>
                    Confirmar
                  </Button>
                )}
                {o.status === "CONFIRMED" && (
                  <Button variant="primary" size="sm" loading={readyMutation.isPending} onClick={() => readyMutation.mutate(o)}>
                    Pronto p/ retirada
                  </Button>
                )}
                {o.status === "READY_FOR_PICKUP" && (
                  <Button variant="primary" size="sm" loading={dispatchMutation.isPending} onClick={() => dispatchMutation.mutate(o)}>
                    Despachar
                  </Button>
                )}
                {o.status === "DISPATCHED" && (
                  <Button variant="primary" size="sm" loading={deliveredMutation.isPending} onClick={() => deliveredMutation.mutate(o)}>
                    Marcar entregue
                  </Button>
                )}
                {!["DELIVERED", "CONCLUDED", "CANCELLED", "CANCELLATION_REQUESTED"].includes(o.status) && (
                  <Button variant="danger" size="sm" onClick={() => setCancelling(o)}>
                    Cancelar
                  </Button>
                )}
                {o.status === "CANCELLATION_REQUESTED" && (
                  <>
                    <Button variant="ghost" size="sm" loading={acceptRefundMutation.isPending} onClick={() => acceptRefundMutation.mutate(o)}>
                      Aceitar reembolso
                    </Button>
                    <Button variant="danger" size="sm" onClick={() => setRejecting(o)}>
                      Recusar reembolso
                    </Button>
                  </>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {cancelling && (
        <CancelOrderModal order={cancelling} onClose={() => setCancelling(null)} onDone={() => { setCancelling(null); refresh(); }} />
      )}

      {rejecting && (
        <RejectRefundModal order={rejecting} onClose={() => setRejecting(null)} onDone={() => { setRejecting(null); refresh(); }} />
      )}
    </section>
  );
}

function CancelOrderModal({
  order,
  onClose,
  onDone,
}: {
  order: KeetaIntegrationOrderResponse;
  onClose: () => void;
  onDone: () => void;
}) {
  const toast = useToast();
  const [reason, setReason] = useState("");
  const [code, setCode] = useState("SYSTEMIC_ISSUES");

  const mutation = useMutation({
    mutationFn: () => requestKeetaOrderCancellation(order.id, { reason: reason.trim(), code, mode: "MANUAL" }),
    onSuccess: () => { toast.success("Cancelamento solicitado à Keeta."); onDone(); },
    onError: (e) => toast.error(apiErrorMessage(e, "Não foi possível solicitar o cancelamento.")),
  });

  return (
    <Modal title={`Cancelar pedido ${order.displayId}`} onClose={onClose}>
      <div style={{ display: "grid", gap: 12 }}>
        <SelectField label="Motivo" value={code} onChange={(e) => setCode(e.target.value)} autoFocus>
          {Object.entries(keetaCancellationReasonLabel).map(([value, label]) => (
            <option key={value} value={value}>{label}</option>
          ))}
        </SelectField>
        <TextField
          label="Detalhes (texto livre)"
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          placeholder="ex.: item em falta no estoque"
        />
        <Button variant="danger" block disabled={reason.trim() === ""} loading={mutation.isPending} onClick={() => mutation.mutate()}>
          Solicitar cancelamento
        </Button>
      </div>
    </Modal>
  );
}

function RejectRefundModal({
  order,
  onClose,
  onDone,
}: {
  order: KeetaIntegrationOrderResponse;
  onClose: () => void;
  onDone: () => void;
}) {
  const toast = useToast();
  const [reason, setReason] = useState("");
  const [code, setCode] = useState("OTHER");

  const mutation = useMutation({
    mutationFn: () => rejectKeetaOrderRefund(order.id, { reason: reason.trim(), code }),
    onSuccess: () => { toast.success("Reembolso recusado."); onDone(); },
    onError: (e) => toast.error(apiErrorMessage(e, "Não foi possível recusar o reembolso.")),
  });

  return (
    <Modal title={`Recusar reembolso — pedido ${order.displayId}`} onClose={onClose}>
      <div style={{ display: "grid", gap: 12 }}>
        <SelectField label="Motivo" value={code} onChange={(e) => setCode(e.target.value)} autoFocus>
          <option value="DISH_ALREADY_DONE">Prato já preparado</option>
          <option value="OUT_FOR_DELIVERY">Já saiu para entrega</option>
          <option value="OTHER">Outro</option>
        </SelectField>
        <TextField
          label="Detalhes"
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          placeholder="explique o motivo da recusa"
        />
        <Button variant="danger" block disabled={reason.trim() === ""} loading={mutation.isPending} onClick={() => mutation.mutate()}>
          Recusar reembolso
        </Button>
      </div>
    </Modal>
  );
}
