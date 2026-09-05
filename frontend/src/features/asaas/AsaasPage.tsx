import { useState } from "react";
import type { CSSProperties, ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AsaasBillingType,
  asaasBillingTypeLabel,
  asaasPaymentStatusLabel,
  createAsaasCustomer,
  createAsaasPayment,
  createAsaasSavedCard,
  createAsaasSetting,
  deleteAsaasCustomer,
  deleteAsaasPayment,
  deleteAsaasSavedCard,
  deleteAsaasSetting,
  deleteAsaasWebhookLog,
  getAllActiveAsaasSettings,
  getAllAsaasCustomersByCompany,
  getAsaasPaymentsByBranch,
  getAsaasSavedCardsByCustomerId,
  getAsaasWebhookLogsByPaymentId,
  getPendingAsaasPaymentsByBranch,
  getUnprocessedAsaasWebhookLogs,
  setDefaultAsaasSavedCard,
  updateAsaasCustomer,
  updateAsaasPayment,
  updateAsaasSetting,
  updateAsaasWebhookLogStatus,
  webhookLogStatusLabel,
  WebhookLogStatus,
  type AsaasCustomerBindingResponse,
  type AsaasPaymentResponse,
  type AsaasSavedCardResponse,
  type AsaasSettingResponse,
  type AsaasWebhookLogResponse,
  type CreateAsaasPaymentPayload,
} from "./api";
import { getCustomersByCompany } from "../customers/api";
import { useAuthStore } from "../../stores/authStore";
import { formatBRL, parseApiDate } from "../../lib/types";
import { ApiError } from "../../lib/apiClient";
import { useToast } from "../../ui/Toast";
import { useDialog } from "../../ui/Dialog";
import { QueryError } from "../../components/QueryError";
import { Button } from "../../ui/Button";
import { SelectField, TextField } from "../../ui/Field";
import { Switch } from "../../ui/Switch";
import { Modal } from "../../ui/Modal";
import { EmptyState } from "../../ui/EmptyState";
import { SkeletonList } from "../../ui/Skeleton";

// Central da integração Asaas (gateway de pagamentos): Configurações, Clientes, Pagamentos,
// Cartões salvos e Webhooks — os 5 controllers do backend (AsaasSettingController,
// AsaasCustomerController, AsaasPaymentController, AsaasSavedCardController,
// AsaasWebhookLogController) reunidos numa única tela em abas, no mesmo padrão visual das
// demais telas do app (ticket/chip/display + Button/Field/Modal + toasts/confirm do SweetAlert2).

type TabId = "settings" | "customers" | "payments" | "savedCards" | "webhooks";

const TABS: Array<{ id: TabId; label: string; icon: string }> = [
  { id: "settings", label: "Configurações", icon: "🔑" },
  { id: "customers", label: "Clientes", icon: "🔗" },
  { id: "payments", label: "Pagamentos", icon: "💳" },
  { id: "savedCards", label: "Cartões salvos", icon: "🗂️" },
  { id: "webhooks", label: "Webhooks", icon: "📬" },
];

function apiErrorMessage(e: unknown, fallback: string): string {
  return e instanceof ApiError ? e.message : fallback;
}

export function AsaasPage() {
  const { companyId: rawCompanyId, branchId } = useAuthStore();
  const companyId = rawCompanyId ?? 1;
  const [tab, setTab] = useState<TabId>("settings");

  return (
    <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }}>
      <div className="rise" style={{ marginBottom: 18 }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>
          Integração Asaas
        </h2>
        <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
          Credenciais, clientes vinculados, cobranças, cartões tokenizados e log de webhooks do
          gateway de pagamentos — somente gerente/administrador.
        </span>
      </div>

      <div
        role="tablist"
        aria-label="Áreas da integração Asaas"
        className="ui-row ui-row-wrap"
        style={{ gap: 4, borderBottom: "1px solid var(--line-soft)", marginBottom: 18 }}
      >
        {TABS.map((t) => (
          <button
            key={t.id}
            type="button"
            role="tab"
            id={`asaas-tab-${t.id}`}
            aria-selected={tab === t.id}
            aria-controls={`asaas-panel-${t.id}`}
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

      <div id={`asaas-panel-${tab}`} role="tabpanel" aria-labelledby={`asaas-tab-${tab}`} className="rise rise-1">
        {tab === "settings" && <SettingsSection companyId={companyId} />}
        {tab === "customers" && <CustomersSection companyId={companyId} />}
        {tab === "payments" && <PaymentsSection companyId={companyId} branchId={branchId} />}
        {tab === "savedCards" && <SavedCardsSection companyId={companyId} />}
        {tab === "webhooks" && <WebhooksSection companyId={companyId} />}
      </div>
    </main>
  );
}

// ============================================================================================
// Configurações
// ============================================================================================

function SettingsSection({ companyId }: { companyId: number }) {
  const queryClient = useQueryClient();
  const toast = useToast();
  const dialog = useDialog();

  const [creating, setCreating] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [branchIdInput, setBranchIdInput] = useState("");
  const [apiKey, setApiKey] = useState("");
  const [webhookToken, setWebhookToken] = useState("");
  const [environment, setEnvironment] = useState("Sandbox");
  const [isActive, setIsActive] = useState(true);

  const settingsQuery = useQuery({
    queryKey: ["asaas", "settings", companyId],
    queryFn: () => getAllActiveAsaasSettings(companyId),
  });

  const refresh = () => void queryClient.invalidateQueries({ queryKey: ["asaas", "settings"] });

  const resetForm = () => {
    setBranchIdInput("");
    setApiKey("");
    setWebhookToken("");
    setEnvironment("Sandbox");
    setIsActive(true);
  };

  const createMutation = useMutation({
    mutationFn: () =>
      createAsaasSetting({
        companyId,
        branchId: branchIdInput.trim() === "" ? null : Number(branchIdInput),
        apiKey: apiKey.trim(),
        webhookToken: webhookToken.trim() === "" ? null : webhookToken.trim(),
        environment,
        isActive,
      }),
    onSuccess: () => {
      toast.success("Configuração criada.");
      setCreating(false);
      resetForm();
      refresh();
    },
    onError: (e) => setError(apiErrorMessage(e, "Não foi possível criar a configuração.")),
  });

  const editing = (settingsQuery.data ?? []).find((s) => s.id === editingId) ?? null;

  const updateMutation = useMutation({
    mutationFn: () =>
      updateAsaasSetting(editingId!, {
        companyId,
        apiKey: apiKey.trim() === "" ? undefined : apiKey.trim(),
        webhookToken: webhookToken.trim() === "" ? undefined : webhookToken.trim(),
        environment,
        isActive,
      }),
    onSuccess: () => {
      toast.success("Configuração atualizada.");
      setEditingId(null);
      resetForm();
      refresh();
    },
    onError: (e) => setError(apiErrorMessage(e, "Não foi possível salvar.")),
  });

  const deleteMutation = useMutation({
    mutationFn: (setting: AsaasSettingResponse) => deleteAsaasSetting(setting.id, companyId),
    onSuccess: () => {
      toast.success("Configuração removida.");
      refresh();
    },
    onError: (e) => toast.error(apiErrorMessage(e, "Não foi possível remover.")),
  });

  const askDelete = async (setting: AsaasSettingResponse) => {
    const scope = setting.branchId ? `da filial ${setting.branchId}` : "padrão da empresa";
    const ok = await dialog.confirm({
      title: "Remover configuração",
      message: `Remover a configuração Asaas ${scope}? Cobranças já criadas continuam válidas, mas novas cobranças dessa unidade deixarão de funcionar até cadastrar outra.`,
      danger: true,
      confirmLabel: "Remover",
    });
    if (ok) deleteMutation.mutate(setting);
  };

  const openEdit = (setting: AsaasSettingResponse) => {
    setError(null);
    setApiKey("");
    setWebhookToken("");
    setEnvironment(setting.environment);
    setIsActive(setting.isActive);
    setEditingId(setting.id);
  };

  const list = settingsQuery.data ?? [];

  return (
    <section style={{ display: "grid", gap: 14 }}>
      <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", alignItems: "center" }}>
        <div style={{ display: "grid", gap: 4, maxWidth: 560 }}>
          <span className="display" style={{ fontSize: "1.2rem" }}>
            Credenciais por empresa/filial
          </span>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>
            Cada filial pode ter sua própria chave de API do Asaas; sem uma específica, cobranças
            dessa filial usam a configuração padrão da empresa (sem filial definida).
          </span>
        </div>
        <Button
          variant="primary"
          onClick={() => {
            setError(null);
            resetForm();
            setCreating(true);
          }}
        >
          + Nova configuração
        </Button>
      </div>

      {settingsQuery.isError && <QueryError error={settingsQuery.error} what="as configurações do Asaas" />}
      {settingsQuery.isLoading && <SkeletonList rows={3} rowHeight={64} />}

      {!settingsQuery.isLoading && list.length === 0 && (
        <EmptyState
          icon="🔑"
          title="Nenhuma configuração cadastrada"
          description="Cadastre a chave de API do Asaas para começar a emitir cobranças."
          action={
            <Button variant="primary" onClick={() => { setError(null); resetForm(); setCreating(true); }}>
              + Nova configuração
            </Button>
          }
        />
      )}

      {list.length > 0 && (
        <div className="ticket" style={{ marginTop: 4 }}>
          {list.map((s) => (
            <div key={s.id} className="ticket-row" style={{ alignItems: "center" }}>
              <div style={{ display: "grid", gap: 2 }}>
                <span style={{ fontWeight: 600 }}>{s.branchId ? `Filial ${s.branchId}` : "Padrão da empresa"}</span>
                <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                  Criada em {parseApiDate(s.createdAt).toLocaleDateString("pt-BR")}
                  {s.updatedAt ? ` · atualizada em ${parseApiDate(s.updatedAt).toLocaleDateString("pt-BR")}` : ""}
                </span>
              </div>
              <div className="ui-row" style={{ gap: 8 }}>
                <span className="chip" style={{ "--dot": s.environment === "Production" ? "var(--danger)" : "var(--busy)" } as CSSProperties}>
                  {s.environment === "Production" ? "Produção" : "Sandbox"}
                </span>
                <span className="chip" style={{ "--dot": s.isActive ? "var(--ok)" : "var(--ink-faint)" } as CSSProperties}>
                  {s.isActive ? "Ativa" : "Inativa"}
                </span>
                <Button variant="ghost" size="sm" onClick={() => openEdit(s)}>
                  Editar
                </Button>
                <Button variant="danger" size="sm" loading={deleteMutation.isPending} onClick={() => void askDelete(s)}>
                  Remover
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      {creating && (
        <Modal title="Nova configuração Asaas" onClose={() => setCreating(false)}>
          <div style={{ display: "grid", gap: 12 }}>
            <TextField
              label="Filial (opcional — vazio = padrão da empresa)"
              inputMode="numeric"
              value={branchIdInput}
              onChange={(e) => setBranchIdInput(e.target.value.replace(/\D/g, ""))}
              placeholder="ex.: 2"
              autoFocus
            />
            <TextField
              label="Chave de API (apiKey)"
              type="password"
              value={apiKey}
              onChange={(e) => setApiKey(e.target.value)}
              placeholder="cole a API Key do Asaas"
              hint="Fica criptografada no banco — esta tela nunca reexibe o valor salvo."
            />
            <TextField
              label="Token do webhook (opcional)"
              type="password"
              value={webhookToken}
              onChange={(e) => setWebhookToken(e.target.value)}
              placeholder="usado para validar a origem dos webhooks"
            />
            <SelectField label="Ambiente" value={environment} onChange={(e) => setEnvironment(e.target.value)}>
              <option value="Sandbox">Sandbox (testes)</option>
              <option value="Production">Produção</option>
            </SelectField>
            <div className="ui-row" style={{ alignItems: "center", gap: 10 }}>
              <Switch checked={isActive} onChange={setIsActive} label="Configuração ativa" />
              <span style={{ fontSize: "0.85rem", color: "var(--ink-faint)" }}>Ativa</span>
            </div>

            {error && <p className="error-text">{error}</p>}

            <Button
              variant="primary"
              block
              disabled={apiKey.trim() === ""}
              loading={createMutation.isPending}
              onClick={() => createMutation.mutate()}
            >
              Criar configuração
            </Button>
          </div>
        </Modal>
      )}

      {editing && (
        <Modal title={`Editar configuração — ${editing.branchId ? `Filial ${editing.branchId}` : "Padrão da empresa"}`} onClose={() => setEditingId(null)}>
          <div style={{ display: "grid", gap: 12 }}>
            <TextField
              label="Nova chave de API (opcional)"
              type="password"
              value={apiKey}
              onChange={(e) => setApiKey(e.target.value)}
              placeholder="deixe em branco para manter a atual"
              autoFocus
            />
            <TextField
              label="Novo token do webhook (opcional)"
              type="password"
              value={webhookToken}
              onChange={(e) => setWebhookToken(e.target.value)}
              placeholder="deixe em branco para manter o atual"
            />
            <SelectField label="Ambiente" value={environment} onChange={(e) => setEnvironment(e.target.value)}>
              <option value="Sandbox">Sandbox (testes)</option>
              <option value="Production">Produção</option>
            </SelectField>
            <div className="ui-row" style={{ alignItems: "center", gap: 10 }}>
              <Switch checked={isActive} onChange={setIsActive} label="Configuração ativa" />
              <span style={{ fontSize: "0.85rem", color: "var(--ink-faint)" }}>{isActive ? "Ativa" : "Inativa"}</span>
            </div>

            {error && <p className="error-text">{error}</p>}

            <Button variant="primary" block loading={updateMutation.isPending} onClick={() => updateMutation.mutate()}>
              Salvar alterações
            </Button>
          </div>
        </Modal>
      )}
    </section>
  );
}

// ============================================================================================
// Clientes
// ============================================================================================

function CustomersSection({ companyId }: { companyId: number }) {
  const queryClient = useQueryClient();
  const toast = useToast();
  const dialog = useDialog();

  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<AsaasCustomerBindingResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [selectedCustomerId, setSelectedCustomerId] = useState("");
  const [asaasCustomerId, setAsaasCustomerId] = useState("");

  const bindingsQuery = useQuery({
    queryKey: ["asaas", "customers", companyId],
    queryFn: () => getAllAsaasCustomersByCompany(companyId),
  });

  const customersQuery = useQuery({
    queryKey: ["customers", companyId, ""],
    queryFn: () => getCustomersByCompany(companyId),
  });

  const customerName = (customerId: number) =>
    customersQuery.data?.find((c) => c.id === customerId)?.name ?? `Cliente #${customerId}`;

  const boundIds = new Set((bindingsQuery.data ?? []).map((b) => b.customerId));
  const unboundCustomers = (customersQuery.data ?? []).filter((c) => !boundIds.has(c.id));

  const refresh = () => void queryClient.invalidateQueries({ queryKey: ["asaas", "customers"] });

  const createMutation = useMutation({
    mutationFn: () =>
      createAsaasCustomer({
        customerId: Number(selectedCustomerId),
        companyId,
        asaasCustomerId: asaasCustomerId.trim(),
      }),
    onSuccess: () => {
      toast.success("Vínculo com o Asaas criado.");
      setCreating(false);
      setSelectedCustomerId("");
      setAsaasCustomerId("");
      refresh();
    },
    onError: (e) => setError(apiErrorMessage(e, "Não foi possível criar o vínculo.")),
  });

  const updateMutation = useMutation({
    mutationFn: () => updateAsaasCustomer(editing!.id, asaasCustomerId.trim()),
    onSuccess: () => {
      toast.success("Vínculo atualizado.");
      setEditing(null);
      setAsaasCustomerId("");
      refresh();
    },
    onError: (e) => setError(apiErrorMessage(e, "Não foi possível salvar.")),
  });

  const deleteMutation = useMutation({
    mutationFn: (b: AsaasCustomerBindingResponse) => deleteAsaasCustomer(companyId, b.customerId),
    onSuccess: () => {
      toast.success("Vínculo removido.");
      refresh();
    },
    onError: (e) => toast.error(apiErrorMessage(e, "Não foi possível remover.")),
  });

  const askDelete = async (b: AsaasCustomerBindingResponse) => {
    const ok = await dialog.confirm({
      title: "Remover vínculo",
      message: `Remover o vínculo de "${customerName(b.customerId)}" com o Asaas? Cobranças já emitidas não são afetadas, mas novas cobranças para este cliente precisarão de um novo vínculo.`,
      danger: true,
      confirmLabel: "Remover",
    });
    if (ok) deleteMutation.mutate(b);
  };

  const list = bindingsQuery.data ?? [];

  return (
    <section style={{ display: "grid", gap: 14 }}>
      <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", alignItems: "center" }}>
        <div style={{ display: "grid", gap: 4, maxWidth: 560 }}>
          <span className="display" style={{ fontSize: "1.2rem" }}>
            Clientes vinculados ao Asaas
          </span>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>
            Cada cliente do SyncBar precisa de um vínculo com um cliente já cadastrado no painel do
            Asaas antes de receber cobranças ou ter um cartão salvo.
          </span>
        </div>
        <Button
          variant="primary"
          disabled={unboundCustomers.length === 0}
          onClick={() => { setError(null); setSelectedCustomerId(""); setAsaasCustomerId(""); setCreating(true); }}
        >
          + Vincular cliente
        </Button>
      </div>

      {bindingsQuery.isError && <QueryError error={bindingsQuery.error} what="os clientes vinculados" />}
      {customersQuery.isError && <QueryError error={customersQuery.error} what="a lista de clientes" />}
      {bindingsQuery.isLoading && <SkeletonList rows={4} rowHeight={58} />}

      {!bindingsQuery.isLoading && list.length === 0 && (
        <EmptyState
          icon="🔗"
          title="Nenhum cliente vinculado"
          description="Vincule um cliente do SyncBar a um cliente já cadastrado no painel do Asaas."
          action={
            unboundCustomers.length > 0 ? (
              <Button variant="primary" onClick={() => { setError(null); setCreating(true); }}>
                + Vincular cliente
              </Button>
            ) : undefined
          }
        />
      )}

      {list.length > 0 && (
        <div className="ticket">
          {list.map((b) => (
            <div key={b.id} className="ticket-row" style={{ alignItems: "center" }}>
              <div style={{ display: "grid", gap: 2 }}>
                <span style={{ fontWeight: 600 }}>{customerName(b.customerId)}</span>
                <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)", fontFamily: "monospace" }}>
                  {b.asaasCustomerId}
                </span>
              </div>
              <div className="ui-row" style={{ gap: 8 }}>
                <span className="chip" style={{ "--dot": b.isActive ? "var(--ok)" : "var(--ink-faint)" } as CSSProperties}>
                  {b.isActive ? "Ativo" : "Inativo"}
                </span>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => { setError(null); setAsaasCustomerId(b.asaasCustomerId); setEditing(b); }}
                >
                  Editar
                </Button>
                <Button variant="danger" size="sm" loading={deleteMutation.isPending} onClick={() => void askDelete(b)}>
                  Remover
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      {creating && (
        <Modal title="Vincular cliente ao Asaas" onClose={() => setCreating(false)}>
          <div style={{ display: "grid", gap: 12 }}>
            <SelectField
              label="Cliente"
              value={selectedCustomerId}
              onChange={(e) => setSelectedCustomerId(e.target.value)}
              autoFocus
            >
              <option value="">Selecione…</option>
              {unboundCustomers.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </SelectField>
            <TextField
              label="ID do cliente no Asaas"
              value={asaasCustomerId}
              onChange={(e) => setAsaasCustomerId(e.target.value)}
              placeholder="ex.: cus_000005219613"
              hint="Cadastre o cliente no painel do Asaas primeiro e cole o ID aqui."
            />

            {error && <p className="error-text">{error}</p>}

            <Button
              variant="primary"
              block
              disabled={selectedCustomerId === "" || asaasCustomerId.trim() === ""}
              loading={createMutation.isPending}
              onClick={() => createMutation.mutate()}
            >
              Vincular
            </Button>
          </div>
        </Modal>
      )}

      {editing && (
        <Modal title={`Editar vínculo — ${customerName(editing.customerId)}`} onClose={() => setEditing(null)}>
          <div style={{ display: "grid", gap: 12 }}>
            <TextField
              label="ID do cliente no Asaas"
              value={asaasCustomerId}
              onChange={(e) => setAsaasCustomerId(e.target.value)}
              autoFocus
            />
            {error && <p className="error-text">{error}</p>}
            <Button
              variant="primary"
              block
              disabled={asaasCustomerId.trim() === ""}
              loading={updateMutation.isPending}
              onClick={() => updateMutation.mutate()}
            >
              Salvar
            </Button>
          </div>
        </Modal>
      )}
    </section>
  );
}

// ============================================================================================
// Pagamentos
// ============================================================================================

const paymentStatusColor = (status: string): string => {
  if (status === "RECEIVED" || status === "CONFIRMED" || status === "RECEIVED_IN_CASH") return "var(--ok)";
  if (status === "OVERDUE" || status.startsWith("CHARGEBACK")) return "var(--danger)";
  return "var(--busy)";
};

function PaymentsSection({ companyId, branchId }: { companyId: number; branchId: number }) {
  const queryClient = useQueryClient();
  const toast = useToast();
  const dialog = useDialog();

  const [pendingOnly, setPendingOnly] = useState(false);
  const [creating, setCreating] = useState(false);
  const [detailsFor, setDetailsFor] = useState<AsaasPaymentResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState<{
    customerId: string;
    customerOrderId: string;
    billingType: string;
    value: string;
    dueDate: string;
    installmentCount: string;
    creditCardToken: string;
  }>({ customerId: "", customerOrderId: "", billingType: AsaasBillingType.Pix, value: "", dueDate: "", installmentCount: "1", creditCardToken: "" });

  const paymentsQuery = useQuery({
    queryKey: ["asaas", "payments", branchId, pendingOnly],
    queryFn: () => (pendingOnly ? getPendingAsaasPaymentsByBranch(branchId) : getAsaasPaymentsByBranch(branchId)),
  });

  const bindingsQuery = useQuery({
    queryKey: ["asaas", "customers", companyId],
    queryFn: () => getAllAsaasCustomersByCompany(companyId),
  });

  const customersQuery = useQuery({
    queryKey: ["customers", companyId, ""],
    queryFn: () => getCustomersByCompany(companyId),
  });

  const customerName = (customerId: number | null) => {
    if (customerId === null) return "Sem cliente vinculado";
    return customersQuery.data?.find((c) => c.id === customerId)?.name ?? `Cliente #${customerId}`;
  };

  const savedCardsForSelectedCustomer = useQuery({
    queryKey: ["asaas", "savedCards", form.customerId],
    queryFn: () => getAsaasSavedCardsByCustomerId(Number(form.customerId)),
    enabled: form.billingType === AsaasBillingType.CreditCard && form.customerId !== "",
  });

  const refresh = () => void queryClient.invalidateQueries({ queryKey: ["asaas", "payments"] });

  const resetForm = () =>
    setForm({ customerId: "", customerOrderId: "", billingType: AsaasBillingType.Pix, value: "", dueDate: "", installmentCount: "1", creditCardToken: "" });

  const createMutation = useMutation({
    mutationFn: () => {
      const payload: CreateAsaasPaymentPayload = {
        branchId,
        customerOrderId: Number(form.customerOrderId),
        customerId: form.customerId === "" ? null : Number(form.customerId),
        billingType: form.billingType,
        value: Number(form.value.replace(",", ".")),
        dueDate: form.dueDate,
        installmentCount: form.billingType === AsaasBillingType.CreditCard ? Number(form.installmentCount) || 1 : 1,
        creditCardToken: form.billingType === AsaasBillingType.CreditCard ? form.creditCardToken || null : null,
      };
      return createAsaasPayment(payload);
    },
    onSuccess: () => {
      toast.success("Cobrança criada no Asaas.");
      setCreating(false);
      resetForm();
      refresh();
    },
    onError: (e) => setError(apiErrorMessage(e, "Não foi possível criar a cobrança.")),
  });

  const deleteMutation = useMutation({
    mutationFn: (p: AsaasPaymentResponse) => deleteAsaasPayment(p.id),
    onSuccess: () => {
      toast.success("Cobrança cancelada.");
      refresh();
    },
    onError: (e) => toast.error(apiErrorMessage(e, "Não foi possível cancelar a cobrança.")),
  });

  const askDelete = async (p: AsaasPaymentResponse) => {
    const ok = await dialog.confirm({
      title: "Cancelar cobrança",
      message: `Cancelar a cobrança ${p.asaasPaymentId} de ${formatBRL(p.value)}? Esta ação cancela a cobrança no Asaas e não pode ser desfeita.`,
      danger: true,
      confirmLabel: "Cancelar cobrança",
    });
    if (ok) deleteMutation.mutate(p);
  };

  const list = paymentsQuery.data ?? [];
  const boundCustomers = (bindingsQuery.data ?? []).filter((b) => b.isActive);

  return (
    <section style={{ display: "grid", gap: 14 }}>
      <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", alignItems: "center" }}>
        <div style={{ display: "grid", gap: 4, maxWidth: 560 }}>
          <span className="display" style={{ fontSize: "1.2rem" }}>
            Cobranças — Filial {branchId}
          </span>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>
            Pix, boleto e cartão de crédito emitidos via Asaas para esta filial.
          </span>
        </div>
        <div className="ui-row" style={{ gap: 14, alignItems: "center" }}>
          <div className="ui-row" style={{ gap: 8, alignItems: "center" }}>
            <Switch checked={pendingOnly} onChange={setPendingOnly} label="Mostrar somente pendentes" />
            <span style={{ fontSize: "0.85rem", color: "var(--ink-faint)" }}>Só pendentes</span>
          </div>
          <Button
            variant="primary"
            disabled={boundCustomers.length === 0}
            title={boundCustomers.length === 0 ? "Vincule ao menos um cliente ao Asaas na aba Clientes" : undefined}
            onClick={() => { setError(null); resetForm(); setCreating(true); }}
          >
            + Nova cobrança
          </Button>
        </div>
      </div>

      {paymentsQuery.isError && <QueryError error={paymentsQuery.error} what="as cobranças" />}
      {paymentsQuery.isLoading && <SkeletonList rows={4} rowHeight={64} />}

      {!paymentsQuery.isLoading && list.length === 0 && (
        <EmptyState
          icon="💳"
          title={pendingOnly ? "Nenhuma cobrança pendente" : "Nenhuma cobrança emitida ainda"}
          description="Crie uma cobrança Pix, boleto ou cartão para um pedido desta filial."
          action={
            boundCustomers.length > 0 ? (
              <Button variant="primary" onClick={() => { setError(null); resetForm(); setCreating(true); }}>
                + Nova cobrança
              </Button>
            ) : undefined
          }
        />
      )}

      {list.length > 0 && (
        <div className="ticket">
          {list.map((p) => (
            <div key={p.id} className="ticket-row" style={{ alignItems: "center" }}>
              <div style={{ display: "grid", gap: 2 }}>
                <span style={{ fontWeight: 600 }}>
                  {formatBRL(p.value)} · Pedido #{p.customerOrderId}
                </span>
                <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                  {customerName(p.customerId)} · vence em{" "}
                  {parseApiDate(p.dueDate).toLocaleDateString("pt-BR", { timeZone: "UTC" })}
                </span>
              </div>
              <div className="ui-row" style={{ gap: 8 }}>
                <span className="chip">{asaasBillingTypeLabel[p.billingType] ?? p.billingType}</span>
                <span className="chip" style={{ "--dot": paymentStatusColor(p.status) } as CSSProperties}>
                  {asaasPaymentStatusLabel[p.status] ?? p.status}
                </span>
                <Button variant="ghost" size="sm" onClick={() => setDetailsFor(p)}>
                  Detalhes
                </Button>
                <Button
                  variant="danger"
                  size="sm"
                  disabled={p.status === "RECEIVED" || p.status === "CONFIRMED"}
                  title={p.status === "RECEIVED" || p.status === "CONFIRMED" ? "Cobranças já liquidadas não podem ser canceladas." : undefined}
                  loading={deleteMutation.isPending}
                  onClick={() => void askDelete(p)}
                >
                  Cancelar
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      {creating && (
        <Modal title="Nova cobrança" onClose={() => setCreating(false)}>
          <div style={{ display: "grid", gap: 12 }}>
            <SelectField
              label="Cliente"
              value={form.customerId}
              onChange={(e) => setForm((f) => ({ ...f, customerId: e.target.value }))}
              autoFocus
              hint="Somente clientes já vinculados ao Asaas (aba Clientes)."
            >
              <option value="">Selecione…</option>
              {boundCustomers.map((b) => (
                <option key={b.customerId} value={b.customerId}>
                  {customerName(b.customerId)}
                </option>
              ))}
            </SelectField>

            <TextField
              label="Nº do pedido (CustomerOrderId)"
              inputMode="numeric"
              value={form.customerOrderId}
              onChange={(e) => setForm((f) => ({ ...f, customerOrderId: e.target.value.replace(/\D/g, "") }))}
              placeholder="ex.: 1024"
            />

            <div className="ui-row ui-row-wrap">
              <div style={{ flex: 1, minWidth: 160 }}>
                <SelectField
                  label="Forma de cobrança"
                  value={form.billingType}
                  onChange={(e) => setForm((f) => ({ ...f, billingType: e.target.value }))}
                >
                  <option value={AsaasBillingType.Pix}>Pix</option>
                  <option value={AsaasBillingType.Boleto}>Boleto</option>
                  <option value={AsaasBillingType.CreditCard}>Cartão de crédito (token salvo)</option>
                </SelectField>
              </div>
              <div style={{ flex: 1, minWidth: 140 }}>
                <TextField
                  label="Valor (R$)"
                  inputMode="decimal"
                  value={form.value}
                  onChange={(e) => setForm((f) => ({ ...f, value: e.target.value }))}
                  placeholder="0,00"
                />
              </div>
              <div style={{ flex: 1, minWidth: 140 }}>
                <TextField
                  label="Vencimento"
                  type="date"
                  value={form.dueDate}
                  onChange={(e) => setForm((f) => ({ ...f, dueDate: e.target.value }))}
                />
              </div>
            </div>

            {form.billingType === AsaasBillingType.CreditCard && (
              <>
                <SelectField
                  label="Cartão salvo do cliente"
                  value={form.creditCardToken}
                  onChange={(e) => setForm((f) => ({ ...f, creditCardToken: e.target.value }))}
                  hint="Cadastre um cartão para o cliente na aba Cartões salvos."
                  disabled={form.customerId === ""}
                >
                  <option value="">Selecione…</option>
                  {(savedCardsForSelectedCustomer.data ?? []).map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.cardBrand} •••• {c.last4Digits} {c.isDefault ? "(padrão)" : ""}
                    </option>
                  ))}
                </SelectField>
                <TextField
                  label="Parcelas"
                  inputMode="numeric"
                  value={form.installmentCount}
                  onChange={(e) => setForm((f) => ({ ...f, installmentCount: e.target.value.replace(/\D/g, "") }))}
                />
              </>
            )}

            {error && <p className="error-text">{error}</p>}

            <Button
              variant="primary"
              block
              disabled={
                form.customerId === "" ||
                form.customerOrderId === "" ||
                form.value.trim() === "" ||
                form.dueDate === "" ||
                (form.billingType === AsaasBillingType.CreditCard && form.creditCardToken === "")
              }
              loading={createMutation.isPending}
              onClick={() => createMutation.mutate()}
            >
              Criar cobrança
            </Button>
          </div>
        </Modal>
      )}

      {detailsFor && (
        <PaymentDetailsModal
          payment={detailsFor}
          onClose={() => setDetailsFor(null)}
          onUpdated={() => { setDetailsFor(null); refresh(); }}
        />
      )}
    </section>
  );
}

function PaymentDetailsModal({
  payment,
  onClose,
  onUpdated,
}: {
  payment: AsaasPaymentResponse;
  onClose: () => void;
  onUpdated: () => void;
}) {
  const toast = useToast();
  const [status, setStatus] = useState(payment.status);
  const [netValue, setNetValue] = useState(payment.netValue?.toString() ?? "");

  const updateMutation = useMutation({
    mutationFn: () =>
      updateAsaasPayment(payment.id, {
        status,
        netValue: netValue.trim() === "" ? null : Number(netValue.replace(",", ".")),
      }),
    onSuccess: () => {
      toast.success("Status atualizado.");
      onUpdated();
    },
    onError: (e) => toast.error(apiErrorMessage(e, "Não foi possível atualizar o status.")),
  });

  const copyPixCode = async () => {
    if (!payment.pixPayload) return;
    try {
      await navigator.clipboard.writeText(payment.pixPayload);
      toast.success("Código Pix copiado.");
    } catch {
      toast.error("Não foi possível copiar — copie manualmente.");
    }
  };

  return (
    <Modal title={`Cobrança ${payment.asaasPaymentId}`} onClose={onClose} wide>
      <div style={{ display: "grid", gap: 16 }}>
        <div className="ui-row ui-row-wrap" style={{ gap: 20 }}>
          <Field2 label="Valor" value={formatBRL(payment.value)} />
          <Field2 label="Forma" value={asaasBillingTypeLabel[payment.billingType] ?? payment.billingType} />
          <Field2 label="Status atual" value={asaasPaymentStatusLabel[payment.status] ?? payment.status} />
          <Field2
            label="Vencimento"
            value={parseApiDate(payment.dueDate).toLocaleDateString("pt-BR", { timeZone: "UTC" })}
          />
          {payment.paymentDate && (
            <Field2 label="Pago em" value={parseApiDate(payment.paymentDate).toLocaleString("pt-BR")} />
          )}
        </div>

        {payment.billingType === "PIX" && payment.pixQrCodeBase64 && (
          <div style={{ display: "grid", gap: 8, justifyItems: "start" }}>
            <span style={{ fontWeight: 600, fontSize: "0.9rem" }}>QR Code Pix</span>
            <img
              src={`data:image/png;base64,${payment.pixQrCodeBase64}`}
              alt="QR Code para pagamento via Pix"
              style={{ width: 180, height: 180, borderRadius: 8, background: "#fff", padding: 8 }}
            />
            {payment.pixPayload && (
              <Button variant="ghost" size="sm" onClick={() => void copyPixCode()}>
                📋 Copiar código copia-e-cola
              </Button>
            )}
          </div>
        )}

        {payment.invoiceUrl && (
          <a href={payment.invoiceUrl} target="_blank" rel="noreferrer" className="btn-ghost" style={{ justifySelf: "start", textDecoration: "none" }}>
            🔗 Ver fatura no Asaas
          </a>
        )}
        {payment.bankSlipUrl && (
          <a href={payment.bankSlipUrl} target="_blank" rel="noreferrer" className="btn-ghost" style={{ justifySelf: "start", textDecoration: "none" }}>
            🧾 Ver boleto
          </a>
        )}

        <div style={{ borderTop: "1px solid var(--line-soft)", paddingTop: 14, display: "grid", gap: 10 }}>
          <span style={{ fontWeight: 600, fontSize: "0.9rem" }}>
            Atualizar status manualmente
          </span>
          <span style={{ color: "var(--ink-faint)", fontSize: "0.82rem" }}>
            Normalmente o status é atualizado sozinho pelo webhook do Asaas — use isto apenas para
            correção manual.
          </span>
          <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "end" }}>
            <div style={{ flex: 1, minWidth: 160 }}>
              <TextField label="Status" value={status} onChange={(e) => setStatus(e.target.value.toUpperCase())} placeholder="ex.: RECEIVED" />
            </div>
            <div style={{ flex: 1, minWidth: 140 }}>
              <TextField
                label="Valor líquido (opcional)"
                inputMode="decimal"
                value={netValue}
                onChange={(e) => setNetValue(e.target.value)}
                placeholder="0,00"
              />
            </div>
            <Button variant="primary" size="sm" loading={updateMutation.isPending} onClick={() => updateMutation.mutate()}>
              Salvar status
            </Button>
          </div>
        </div>
      </div>
    </Modal>
  );
}

function Field2({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div style={{ display: "grid", gap: 2 }}>
      <span style={{ fontSize: "0.78rem", color: "var(--ink-faint)" }}>{label}</span>
      <span style={{ fontWeight: 600 }}>{value}</span>
    </div>
  );
}

// ============================================================================================
// Cartões salvos
// ============================================================================================

function SavedCardsSection({ companyId }: { companyId: number }) {
  const queryClient = useQueryClient();
  const toast = useToast();
  const dialog = useDialog();

  const [customerId, setCustomerId] = useState("");
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [cardForm, setCardForm] = useState({ holderName: "", cardNumber: "", expiryMonth: "", expiryYear: "", ccv: "", setAsDefault: false });

  const bindingsQuery = useQuery({
    queryKey: ["asaas", "customers", companyId],
    queryFn: () => getAllAsaasCustomersByCompany(companyId),
  });

  const customersQuery = useQuery({
    queryKey: ["customers", companyId, ""],
    queryFn: () => getCustomersByCompany(companyId),
  });

  const customerName = (id: number) => customersQuery.data?.find((c) => c.id === id)?.name ?? `Cliente #${id}`;
  const boundCustomers = (bindingsQuery.data ?? []).filter((b) => b.isActive);

  const cardsQuery = useQuery({
    queryKey: ["asaas", "savedCards", customerId],
    queryFn: () => getAsaasSavedCardsByCustomerId(Number(customerId)),
    enabled: customerId !== "",
  });

  const refresh = () => void queryClient.invalidateQueries({ queryKey: ["asaas", "savedCards", customerId] });

  const resetCardForm = () => setCardForm({ holderName: "", cardNumber: "", expiryMonth: "", expiryYear: "", ccv: "", setAsDefault: false });

  const createMutation = useMutation({
    mutationFn: () =>
      createAsaasSavedCard({
        customerId: Number(customerId),
        companyId,
        holderName: cardForm.holderName.trim(),
        cardNumber: cardForm.cardNumber.replace(/\s/g, ""),
        expiryMonth: cardForm.expiryMonth.trim(),
        expiryYear: cardForm.expiryYear.trim(),
        ccv: cardForm.ccv.trim(),
        setAsDefault: cardForm.setAsDefault,
      }),
    onSuccess: () => {
      toast.success("Cartão salvo com sucesso.");
      setCreating(false);
      resetCardForm();
      refresh();
    },
    onError: (e) => setError(apiErrorMessage(e, "Não foi possível salvar o cartão.")),
  });

  const setDefaultMutation = useMutation({
    mutationFn: (card: AsaasSavedCardResponse) => setDefaultAsaasSavedCard(card.id, Number(customerId), companyId),
    onSuccess: () => {
      toast.success("Cartão definido como padrão.");
      refresh();
    },
    onError: (e) => toast.error(apiErrorMessage(e, "Não foi possível definir como padrão.")),
  });

  const deleteMutation = useMutation({
    mutationFn: (card: AsaasSavedCardResponse) => deleteAsaasSavedCard(card.id, Number(customerId), companyId),
    onSuccess: () => {
      toast.success("Cartão removido.");
      refresh();
    },
    onError: (e) => toast.error(apiErrorMessage(e, "Não foi possível remover o cartão.")),
  });

  const askDelete = async (card: AsaasSavedCardResponse) => {
    const ok = await dialog.confirm({
      title: "Remover cartão",
      message: `Remover o cartão ${card.cardBrand} terminado em ${card.last4Digits}?`,
      danger: true,
      confirmLabel: "Remover",
    });
    if (ok) deleteMutation.mutate(card);
  };

  const list = cardsQuery.data ?? [];

  return (
    <section style={{ display: "grid", gap: 14 }}>
      <div style={{ display: "grid", gap: 4, maxWidth: 620 }}>
        <span className="display" style={{ fontSize: "1.2rem" }}>
          Cartões de crédito tokenizados
        </span>
        <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>
          O número do cartão nunca é armazenado no SyncBar — apenas a bandeira, os 4 últimos
          dígitos e o token gerado pelo Asaas, usado para cobranças futuras sem pedir o cartão de
          novo.
        </span>
      </div>

      <div style={{ maxWidth: 360 }}>
        <SelectField label="Cliente" value={customerId} onChange={(e) => setCustomerId(e.target.value)}>
          <option value="">Selecione um cliente vinculado ao Asaas…</option>
          {boundCustomers.map((b) => (
            <option key={b.customerId} value={b.customerId}>
              {customerName(b.customerId)}
            </option>
          ))}
        </SelectField>
      </div>

      {bindingsQuery.isError && <QueryError error={bindingsQuery.error} what="os clientes vinculados" />}

      {customerId === "" && (
        <EmptyState icon="🗂️" title="Selecione um cliente" description="Escolha um cliente acima para ver ou cadastrar cartões salvos." />
      )}

      {customerId !== "" && (
        <>
          <div className="ui-row" style={{ justifyContent: "flex-end" }}>
            <Button variant="primary" onClick={() => { setError(null); resetCardForm(); setCreating(true); }}>
              + Novo cartão
            </Button>
          </div>

          {cardsQuery.isError && <QueryError error={cardsQuery.error} what="os cartões salvos" />}
          {cardsQuery.isLoading && <SkeletonList rows={2} rowHeight={58} />}

          {!cardsQuery.isLoading && list.length === 0 && (
            <EmptyState
              icon="💳"
              title="Nenhum cartão salvo"
              description="Cadastre o primeiro cartão deste cliente."
              action={
                <Button variant="primary" onClick={() => { setError(null); resetCardForm(); setCreating(true); }}>
                  + Novo cartão
                </Button>
              }
            />
          )}

          {list.length > 0 && (
            <div className="ticket">
              {list.map((c) => (
                <div key={c.id} className="ticket-row" style={{ alignItems: "center" }}>
                  <div style={{ display: "grid", gap: 2 }}>
                    <span style={{ fontWeight: 600 }}>
                      {c.cardBrand} •••• {c.last4Digits}
                    </span>
                    <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                      {c.holderName} · vence {c.expiryMonth}/{c.expiryYear}
                    </span>
                  </div>
                  <div className="ui-row" style={{ gap: 8 }}>
                    {c.isDefault && <span className="chip" style={{ "--dot": "var(--ok)" } as CSSProperties}>Padrão</span>}
                    {!c.isDefault && (
                      <Button variant="ghost" size="sm" loading={setDefaultMutation.isPending} onClick={() => setDefaultMutation.mutate(c)}>
                        Tornar padrão
                      </Button>
                    )}
                    <Button variant="danger" size="sm" loading={deleteMutation.isPending} onClick={() => void askDelete(c)}>
                      Remover
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </>
      )}

      {creating && (
        <Modal title="Novo cartão" onClose={() => setCreating(false)}>
          <div style={{ display: "grid", gap: 12 }}>
            <TextField
              label="Nome no cartão"
              value={cardForm.holderName}
              onChange={(e) => setCardForm((f) => ({ ...f, holderName: e.target.value }))}
              autoComplete="cc-name"
              autoFocus
            />
            <TextField
              label="Número do cartão"
              inputMode="numeric"
              value={cardForm.cardNumber}
              onChange={(e) => setCardForm((f) => ({ ...f, cardNumber: e.target.value }))}
              autoComplete="cc-number"
              placeholder="0000 0000 0000 0000"
            />
            <div className="ui-row ui-row-wrap">
              <div style={{ flex: 1, minWidth: 100 }}>
                <TextField
                  label="Mês (MM)"
                  inputMode="numeric"
                  maxLength={2}
                  value={cardForm.expiryMonth}
                  onChange={(e) => setCardForm((f) => ({ ...f, expiryMonth: e.target.value.replace(/\D/g, "") }))}
                  autoComplete="cc-exp-month"
                  placeholder="12"
                />
              </div>
              <div style={{ flex: 1, minWidth: 100 }}>
                <TextField
                  label="Ano (AAAA)"
                  inputMode="numeric"
                  maxLength={4}
                  value={cardForm.expiryYear}
                  onChange={(e) => setCardForm((f) => ({ ...f, expiryYear: e.target.value.replace(/\D/g, "") }))}
                  autoComplete="cc-exp-year"
                  placeholder="2030"
                />
              </div>
              <div style={{ flex: 1, minWidth: 100 }}>
                <TextField
                  label="CVV"
                  inputMode="numeric"
                  maxLength={4}
                  value={cardForm.ccv}
                  onChange={(e) => setCardForm((f) => ({ ...f, ccv: e.target.value.replace(/\D/g, "") }))}
                  autoComplete="cc-csc"
                  placeholder="123"
                />
              </div>
            </div>
            <div className="ui-row" style={{ alignItems: "center", gap: 10 }}>
              <Switch
                checked={cardForm.setAsDefault}
                onChange={(v) => setCardForm((f) => ({ ...f, setAsDefault: v }))}
                label="Definir como cartão padrão"
              />
              <span style={{ fontSize: "0.85rem", color: "var(--ink-faint)" }}>Definir como padrão</span>
            </div>

            {error && <p className="error-text">{error}</p>}

            <Button
              variant="primary"
              block
              disabled={
                cardForm.holderName.trim() === "" ||
                cardForm.cardNumber.trim() === "" ||
                cardForm.expiryMonth.trim() === "" ||
                cardForm.expiryYear.trim() === "" ||
                cardForm.ccv.trim() === ""
              }
              loading={createMutation.isPending}
              onClick={() => createMutation.mutate()}
            >
              Salvar cartão
            </Button>
          </div>
        </Modal>
      )}
    </section>
  );
}

// ============================================================================================
// Webhooks
// ============================================================================================

function webhookStatusColor(status: number): string {
  if (status === WebhookLogStatus.Processed) return "var(--ok)";
  if (status === WebhookLogStatus.Failed) return "var(--danger)";
  return "var(--busy)";
}

function WebhooksSection({ companyId }: { companyId: number }) {
  const queryClient = useQueryClient();
  const toast = useToast();
  const dialog = useDialog();

  const [paymentIdSearch, setPaymentIdSearch] = useState("");
  const [activeSearch, setActiveSearch] = useState<string | null>(null);
  const [viewing, setViewing] = useState<AsaasWebhookLogResponse | null>(null);

  const unprocessedQuery = useQuery({
    queryKey: ["asaas", "webhooks", "unprocessed", companyId],
    queryFn: () => getUnprocessedAsaasWebhookLogs(companyId, 50),
    enabled: activeSearch === null,
  });

  const searchQuery = useQuery({
    queryKey: ["asaas", "webhooks", "by-payment", companyId, activeSearch],
    queryFn: () => getAsaasWebhookLogsByPaymentId(companyId, activeSearch!),
    enabled: activeSearch !== null,
  });

  const query = activeSearch === null ? unprocessedQuery : searchQuery;
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ["asaas", "webhooks", "unprocessed"] });
    void queryClient.invalidateQueries({ queryKey: ["asaas", "webhooks", "by-payment"] });
  };

  const markMutation = useMutation({
    mutationFn: (vars: { log: AsaasWebhookLogResponse; status: number; errorMessage?: string | null }) =>
      updateAsaasWebhookLogStatus(vars.log.id, companyId, vars.status, vars.errorMessage),
    onSuccess: () => {
      toast.success("Status do webhook atualizado.");
      refresh();
    },
    onError: (e) => toast.error(apiErrorMessage(e, "Não foi possível atualizar o status.")),
  });

  const deleteMutation = useMutation({
    mutationFn: (log: AsaasWebhookLogResponse) => deleteAsaasWebhookLog(log.id, companyId),
    onSuccess: () => {
      toast.success("Log removido.");
      refresh();
    },
    onError: (e) => toast.error(apiErrorMessage(e, "Não foi possível remover.")),
  });

  const markFailed = async (log: AsaasWebhookLogResponse) => {
    const reason = await dialog.prompt({
      title: "Marcar como falha",
      label: "Motivo da falha",
      placeholder: "ex.: pedido não encontrado",
    });
    if (reason === null) return;
    markMutation.mutate({ log, status: WebhookLogStatus.Failed, errorMessage: reason });
  };

  const askDelete = async (log: AsaasWebhookLogResponse) => {
    const ok = await dialog.confirm({
      title: "Remover log de webhook",
      message: `Remover o log do evento "${log.event}"? Isso não afeta o processamento já realizado.`,
      danger: true,
      confirmLabel: "Remover",
    });
    if (ok) deleteMutation.mutate(log);
  };

  const list = query.data ?? [];

  return (
    <section style={{ display: "grid", gap: 14 }}>
      <div style={{ display: "grid", gap: 4, maxWidth: 620 }}>
        <span className="display" style={{ fontSize: "1.2rem" }}>
          Eventos recebidos do Asaas
        </span>
        <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>
          Trilha de auditoria de todo evento (pagamento recebido, vencido, estornado…) que o Asaas
          enviou por webhook — útil para investigar por que uma cobrança não atualizou sozinha.
        </span>
      </div>

      <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "end" }}>
        <div style={{ flex: 1, minWidth: 220 }}>
          <TextField
            label="Buscar por ID da cobrança no Asaas"
            value={paymentIdSearch}
            onChange={(e) => setPaymentIdSearch(e.target.value)}
            placeholder="ex.: pay_080225823729"
          />
        </div>
        <Button variant="ghost" disabled={paymentIdSearch.trim() === ""} onClick={() => setActiveSearch(paymentIdSearch.trim())}>
          Buscar
        </Button>
        {activeSearch !== null && (
          <Button variant="ghost" onClick={() => { setActiveSearch(null); setPaymentIdSearch(""); }}>
            Voltar para não processados
          </Button>
        )}
      </div>

      {query.isError && <QueryError error={query.error} what="os webhooks" />}
      {query.isLoading && <SkeletonList rows={4} rowHeight={64} />}

      {!query.isLoading && list.length === 0 && (
        <EmptyState
          icon="📬"
          title={activeSearch ? "Nenhum evento encontrado para essa cobrança" : "Nenhum evento pendente"}
          description={activeSearch ? "Confira se o ID da cobrança está correto." : "Todos os webhooks recebidos já foram processados."}
        />
      )}

      {list.length > 0 && (
        <div className="ticket">
          {list.map((log) => (
            <div key={log.id} className="ticket-row" style={{ alignItems: "center" }}>
              <div style={{ display: "grid", gap: 2 }}>
                <span style={{ fontWeight: 600 }}>{log.event}</span>
                <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                  {log.paymentId ?? "sem cobrança associada"} · {parseApiDate(log.createdAt).toLocaleString("pt-BR")}
                </span>
                {log.errorMessage && (
                  <span style={{ fontSize: "0.78rem", color: "var(--danger)" }}>{log.errorMessage}</span>
                )}
              </div>
              <div className="ui-row" style={{ gap: 8 }}>
                <span className="chip" style={{ "--dot": webhookStatusColor(log.status) } as CSSProperties}>
                  {webhookLogStatusLabel[log.status] ?? "Desconhecido"}
                </span>
                <Button variant="ghost" size="sm" onClick={() => setViewing(log)}>
                  Ver payload
                </Button>
                {log.status !== WebhookLogStatus.Processed && (
                  <Button
                    variant="ghost"
                    size="sm"
                    loading={markMutation.isPending}
                    onClick={() => markMutation.mutate({ log, status: WebhookLogStatus.Processed })}
                  >
                    Marcar processado
                  </Button>
                )}
                {log.status !== WebhookLogStatus.Failed && (
                  <Button variant="ghost" size="sm" onClick={() => void markFailed(log)}>
                    Marcar falha
                  </Button>
                )}
                <Button variant="danger" size="sm" loading={deleteMutation.isPending} onClick={() => void askDelete(log)}>
                  Remover
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      {viewing && <WebhookPayloadModal log={viewing} onClose={() => setViewing(null)} />}
    </section>
  );
}

function WebhookPayloadModal({ log, onClose }: { log: AsaasWebhookLogResponse; onClose: () => void }) {
  let pretty = log.payload;
  try {
    pretty = JSON.stringify(JSON.parse(log.payload), null, 2);
  } catch {
    // payload não é JSON válido — mostra cru mesmo
  }

  return (
    <Modal title={`Payload — ${log.event}`} onClose={onClose} wide>
      <div style={{ display: "grid", gap: 12 }}>
        <div className="ui-row ui-row-wrap" style={{ gap: 20 }}>
          <Field2 label="Evento Asaas" value={log.asaasEventId ?? "—"} />
          <Field2 label="Cobrança" value={log.paymentId ?? "—"} />
          <Field2 label="IP de origem" value={log.ipAddress ?? "—"} />
          <Field2 label="Recebido em" value={parseApiDate(log.createdAt).toLocaleString("pt-BR")} />
          {log.processedAt && <Field2 label="Processado em" value={parseApiDate(log.processedAt).toLocaleString("pt-BR")} />}
        </div>
        <pre
          style={{
            background: "var(--bg-press)",
            border: "1px solid var(--line-soft)",
            borderRadius: 8,
            padding: 14,
            fontSize: "0.82rem",
            maxHeight: 360,
            overflow: "auto",
            whiteSpace: "pre-wrap",
            wordBreak: "break-word",
          }}
        >
          {pretty}
        </pre>
      </div>
    </Modal>
  );
}
