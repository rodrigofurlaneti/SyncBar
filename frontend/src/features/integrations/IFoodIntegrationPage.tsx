import { useEffect, useState, type CSSProperties } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createIFoodInterruption,
  deleteIFoodInterruption,
  getIFoodFinancialSummary,
  getIFoodInterruptions,
  getIFoodMerchantMappings,
  getIFoodMerchantStatus,
  getIFoodOpeningHours,
  getIFoodSettings,
  saveIFoodOpeningHours,
  saveIFoodSettings,
  setIFoodMerchantMapping,
  setIFoodPreparationTime,
  syncIFoodCatalog,
  syncIFoodFinancial,
  testIFoodConnection,
  type IFoodOpeningHourShift,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { useToast } from "../../ui/Toast";
import { Field, SelectField, TextField } from "../../ui/Field";
import { Button } from "../../ui/Button";
import { Switch } from "../../ui/Switch";
import { QueryError } from "../../components/QueryError";

export function IFoodIntegrationPage() {
  const queryClient = useQueryClient();
  const toast = useToast();
  const { companyId: rawCompanyId } = useAuthStore();
  const companyId = rawCompanyId ?? 1;

  const [clientId, setClientId] = useState("");
  const [clientSecret, setClientSecret] = useState("");
  const [enabled, setEnabled] = useState(false);
  const [ifoodCustomerId, setIfoodCustomerId] = useState("");
  const [initializedCompanyId, setInitializedCompanyId] = useState<number | null>(null);

  const settingsQuery = useQuery({
    queryKey: ["integrations", "ifood", "settings", companyId],
    queryFn: () => getIFoodSettings(companyId),
  });

  // Só preenche o formulário a partir do servidor UMA vez por empresa — depois disso o
  // usuário é dono do que está digitado (evita apagar edição em andamento se a query refizer
  // fetch em segundo plano, ex.: ao voltar o foco pra aba).
  useEffect(() => {
    if (settingsQuery.data && initializedCompanyId !== companyId) {
      setClientId(settingsQuery.data.clientId ?? "");
      setEnabled(settingsQuery.data.enabled);
      setIfoodCustomerId(settingsQuery.data.ifoodCustomerId ?? "");
      setInitializedCompanyId(companyId);
    }
  }, [settingsQuery.data, companyId, initializedCompanyId]);

  const saveMutation = useMutation({
    mutationFn: () =>
      saveIFoodSettings({
        companyId,
        clientId: clientId.trim(),
        clientSecret: clientSecret.trim(),
        enabled,
        ifoodCustomerId: ifoodCustomerId.trim(),
      }),
    onSuccess: () => {
      toast.success("Credenciais do iFood salvas.");
      setClientSecret(""); // nunca reexibe o segredo — limpa o campo após salvar
      void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "settings"] });
    },
    onError: () => toast.error("Não foi possível salvar as credenciais."),
  });

  const testMutation = useMutation({
    mutationFn: () => testIFoodConnection(companyId),
    onSuccess: (result) => {
      if (result.success) toast.success("Conectado ao iFood com sucesso.");
      else toast.error(result.errorMessage ?? "Falha ao conectar — confira as credenciais.");
      void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "settings"] });
    },
    onError: () => toast.error("Não foi possível testar a conexão."),
  });

  const mappingsQuery = useQuery({
    queryKey: ["integrations", "ifood", "merchants", companyId],
    queryFn: () => getIFoodMerchantMappings(companyId),
  });

  const syncCatalogMutation = useMutation({
    mutationFn: () => syncIFoodCatalog(companyId),
    onSuccess: (summary) => {
      if (summary.skipped) {
        toast.error("Integração desabilitada ou nenhuma loja com Merchant ID configurado — nada foi sincronizado.");
        return;
      }
      const errorSuffix = summary.errors > 0 ? ` (${summary.errors} erro${summary.errors === 1 ? "" : "s"})` : "";
      toast.success(
        `Cardápio sincronizado: ${summary.productsSynced} produto(s) em ${summary.branchesSynced} loja(s)${errorSuffix}.`,
      );
    },
    onError: () => toast.error("Não foi possível sincronizar o cardápio com o iFood."),
  });

  // Financeiro é por FILIAL (o repasse do iFood é por loja) — usa a primeira loja já mapeada
  // com MerchantId como padrão. Seletor de loja fica pra quando houver mais de uma configurada
  // com frequência (hoje o usuário de teste tem só a "Matriz").
  const firstMappedBranch = (mappingsQuery.data ?? []).find((m) => !!m.merchantId);

  const financialSummaryQuery = useQuery({
    queryKey: ["integrations", "ifood", "financial", firstMappedBranch?.branchId],
    queryFn: () => getIFoodFinancialSummary(firstMappedBranch!.branchId),
    enabled: !!firstMappedBranch,
  });

  const syncFinancialMutation = useMutation({
    mutationFn: () => syncIFoodFinancial(companyId),
    onSuccess: () => {
      toast.success("Sincronização financeira disparada — os dados aparecem aqui em instantes.");
      void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "financial"] });
    },
    onError: () => toast.error("Não foi possível disparar a sincronização financeira."),
  });

  const lastTest = settingsQuery.data?.lastConnectionTestSucceeded;
  const statusLabel =
    lastTest === true ? "Conectado" : lastTest === false ? "Falhou no último teste" : "Nunca testado";
  const statusDot = lastTest === true ? "var(--ok)" : lastTest === false ? "var(--danger)" : "var(--ink-faint)";

  return (
    <main style={{ padding: 22, maxWidth: 900, margin: "0 auto" }}>
      <div className="rise" style={{ marginBottom: 18 }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>
          Integração iFood
        </h2>
        <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
          credenciais do app (por empresa) + lojas conectadas — somente gerente/administrador
        </span>
      </div>

      {settingsQuery.isError && <QueryError error={settingsQuery.error} what="a integração com o iFood" />}

      <section className="ticket rise rise-1" style={{ padding: 20, display: "grid", gap: 16 }}>
        <div style={{ display: "grid", gap: 4 }}>
          <span className="display" style={{ fontSize: "1.2rem" }}>
            Credenciais do aplicativo
          </span>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.92rem" }}>
            Client ID e Client Secret vêm de "Meus aplicativos" no portal do iFood Developer —
            um único app pode dar acesso a várias lojas, então essas credenciais valem pra
            empresa inteira (cada loja individual é configurada na seção "Lojas" abaixo).
          </span>
        </div>

        <TextField
          label="Client ID"
          value={clientId}
          onChange={(e) => setClientId(e.target.value)}
          placeholder="ex.: 3f9a1c2b-..."
        />

        <TextField
          label="Client Secret"
          type="password"
          value={clientSecret}
          onChange={(e) => setClientSecret(e.target.value)}
          placeholder={settingsQuery.data?.hasCredentials ? "•••••••• (deixe em branco para manter o atual)" : "cole o Client Secret aqui"}
          hint="Fica criptografado no banco — esta tela nunca reexibe o valor já salvo."
        />

        <TextField
          label="iFood Customer ID (opcional)"
          value={ifoodCustomerId}
          onChange={(e) => setIfoodCustomerId(e.target.value)}
          placeholder="necessário só para configurar tempo de preparo (seção Operação da loja)"
          hint="Não é segredo — fica salvo em texto puro. Sem ele, o resto da integração funciona normalmente, só o campo de tempo de preparo fica desabilitado."
        />

        <Field label="Integração ativa">
          {() => (
            <Switch
              checked={enabled}
              onChange={setEnabled}
              label="Ativar integração com o iFood"
              disabled={saveMutation.isPending}
            />
          )}
        </Field>

        <Button
          variant="primary"
          loading={saveMutation.isPending}
          disabled={enabled && clientId.trim() === ""}
          onClick={() => saveMutation.mutate()}
        >
          Salvar credenciais
        </Button>
      </section>

      <section className="ticket rise rise-2" style={{ padding: 20, display: "grid", gap: 12, marginTop: 16 }}>
        <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", gap: 16 }}>
          <div style={{ display: "grid", gap: 4, maxWidth: 520 }}>
            <span className="display" style={{ fontSize: "1.2rem" }}>
              Conexão
            </span>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.92rem" }}>
              Testa as credenciais salvas contra o iFood (autenticação OAuth2). Só funciona
              depois que você tiver credenciais reais de teste/sandbox ou produção.
            </span>
          </div>
          <div className="ui-row" style={{ gap: 12 }}>
            <span className="chip" style={{ "--dot": statusDot } as CSSProperties}>
              {statusLabel}
            </span>
            <Button
              variant="ghost"
              loading={testMutation.isPending}
              disabled={!settingsQuery.data?.hasCredentials}
              onClick={() => testMutation.mutate()}
            >
              Testar conexão
            </Button>
          </div>
        </div>
        {settingsQuery.data?.lastConnectionTestAt && (
          <span style={{ color: "var(--ink-faint)", fontSize: "0.82rem" }}>
            Último teste: {new Date(settingsQuery.data.lastConnectionTestAt).toLocaleString("pt-BR")}
          </span>
        )}
      </section>

      <section className="ticket rise rise-3" style={{ padding: 20, display: "grid", gap: 14, marginTop: 16 }}>
        <div style={{ display: "grid", gap: 4 }}>
          <span className="display" style={{ fontSize: "1.2rem" }}>
            Lojas (merchants)
          </span>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.92rem" }}>
            Cada filial precisa do seu MerchantId do iFood — encontrado na tela "Permissões" do
            seu app no portal (ou em "Testes" → dados da loja de teste). O MerchantUuid é usado
            só em algumas chamadas específicas; pode deixar em branco se não tiver ainda.
          </span>
        </div>

        {mappingsQuery.isError && <QueryError error={mappingsQuery.error} what="as lojas" />}
        {!mappingsQuery.isLoading && (mappingsQuery.data?.length ?? 0) === 0 && (
          <span style={{ color: "var(--ink-faint)", fontSize: "0.88rem" }}>
            Nenhuma filial ativa cadastrada ainda.
          </span>
        )}

        {(mappingsQuery.data ?? []).map((mapping) => (
          <MerchantMappingRow
            key={mapping.branchId}
            mapping={mapping}
            onSaved={() => void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "merchants"] })}
          />
        ))}
      </section>

      <section className="ticket rise rise-3" style={{ padding: 20, display: "grid", gap: 12, marginTop: 16 }}>
        <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", gap: 16, alignItems: "center" }}>
          <div style={{ display: "grid", gap: 4 }}>
            <span className="display" style={{ fontSize: "1.2rem" }}>
              Pedidos
            </span>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.92rem" }}>
              Recebimento automático (a cada 30s), confirmação dentro do prazo de 8 minutos, e
              avanço manual de status (iniciar preparo, pronto, cancelar) pela tela de pedidos.
            </span>
          </div>
          <Link to="/integracoes/ifood/pedidos">
            <Button variant="ghost">Ver pedidos iFood</Button>
          </Link>
        </div>
      </section>

      <section className="ticket rise rise-3" style={{ padding: 20, display: "grid", gap: 12, marginTop: 16 }}>
        <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", gap: 16, alignItems: "center" }}>
          <div style={{ display: "grid", gap: 4, maxWidth: 520 }}>
            <span className="display" style={{ fontSize: "1.2rem" }}>
              Cardápio
            </span>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.92rem" }}>
              Categorias e produtos ativos são enviados sozinhos pro iFood sempre que você
              cria, edita ou desativa algo em Produtos. Use o botão ao lado pra reenviar tudo de
              uma vez (primeira carga, ou depois de uma falha).
            </span>
          </div>
          <Button variant="ghost" loading={syncCatalogMutation.isPending} onClick={() => syncCatalogMutation.mutate()}>
            Sincronizar agora
          </Button>
        </div>
      </section>

      <section className="ticket rise rise-3" style={{ padding: 20, display: "grid", gap: 14, marginTop: 16 }}>
        <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", gap: 16, alignItems: "center" }}>
          <div style={{ display: "grid", gap: 4, maxWidth: 520 }}>
            <span className="display" style={{ fontSize: "1.2rem" }}>
              Financeiro
            </span>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.92rem" }}>
              Trilha de auditoria dos lançamentos e repasses do iFood (últimos 30 dias) — não
              substitui o fechamento de caixa do SyncBar, é só pra conferir o que o iFood
              calculou. Sincroniza sozinho 1x por dia.
            </span>
          </div>
          <Button
            variant="ghost"
            loading={syncFinancialMutation.isPending}
            disabled={!firstMappedBranch}
            onClick={() => syncFinancialMutation.mutate()}
          >
            Sincronizar agora
          </Button>
        </div>

        {!firstMappedBranch && (
          <span style={{ color: "var(--ink-faint)", fontSize: "0.88rem" }}>
            Configure o Merchant ID de ao menos uma loja acima para ver o financeiro.
          </span>
        )}

        {financialSummaryQuery.isError && (
          <QueryError error={financialSummaryQuery.error} what="o financeiro do iFood" />
        )}

        {financialSummaryQuery.data && (
          <>
            <div className="ui-row ui-row-wrap" style={{ gap: 20 }}>
              <div style={{ display: "grid", gap: 2 }}>
                <span style={{ color: "var(--ink-faint)", fontSize: "0.82rem" }}>Lançamentos c/ impacto no repasse</span>
                <span className="display" style={{ fontSize: "1.15rem" }}>
                  {financialSummaryQuery.data.totalFinancialEventsWithTransferImpact.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}
                </span>
              </div>
              <div style={{ display: "grid", gap: 2 }}>
                <span style={{ color: "var(--ink-faint)", fontSize: "0.82rem" }}>Repasses (Settlement)</span>
                <span className="display" style={{ fontSize: "1.15rem" }}>
                  {financialSummaryQuery.data.totalSettlements.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}
                </span>
              </div>
              {financialSummaryQuery.data.hasDiscrepancy && (
                <span className="chip" style={{ "--dot": "var(--danger)" } as CSSProperties}>
                  Revisar conciliação — diferença de{" "}
                  {financialSummaryQuery.data.discrepancyAmount.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}
                </span>
              )}
            </div>

            {financialSummaryQuery.data.settlements.length > 0 && (
              <div style={{ display: "grid", gap: 6 }}>
                <span style={{ color: "var(--ink-dim)", fontSize: "0.88rem", fontWeight: 600 }}>Repasses</span>
                {financialSummaryQuery.data.settlements.map((s) => (
                  <div
                    key={s.id}
                    className="ui-row ui-row-wrap"
                    style={{ justifyContent: "space-between", gap: 10, borderTop: "1px solid var(--line-soft)", paddingTop: 8, fontSize: "0.88rem" }}
                  >
                    <span>
                      {s.type}
                      {s.product ? ` — ${s.product}` : ""}
                    </span>
                    <span style={{ color: "var(--ink-faint)" }}>{s.status}</span>
                    <span>{s.paymentDate ? new Date(s.paymentDate).toLocaleDateString("pt-BR") : "sem data prevista"}</span>
                    <strong>{s.amount.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}</strong>
                  </div>
                ))}
              </div>
            )}

            {financialSummaryQuery.data.events.length === 0 && financialSummaryQuery.data.settlements.length === 0 && (
              <span style={{ color: "var(--ink-faint)", fontSize: "0.88rem" }}>
                Nenhum lançamento financeiro nos últimos 30 dias ainda.
              </span>
            )}
          </>
        )}
      </section>

      {firstMappedBranch && <MerchantOperationsSection branchId={firstMappedBranch.branchId} />}
      {!firstMappedBranch && (
        <section className="ticket rise rise-3" style={{ padding: 20, display: "grid", gap: 8, marginTop: 16 }}>
          <span className="display" style={{ fontSize: "1.1rem" }}>
            Operação da loja
          </span>
          <span style={{ color: "var(--ink-faint)", fontSize: "0.88rem" }}>
            Configure o Merchant ID de ao menos uma loja acima para ver status, interrupções,
            horários e tempo de preparo.
          </span>
        </section>
      )}

      <section className="ticket rise rise-3" style={{ padding: 20, display: "grid", gap: 8, marginTop: 16 }}>
        <span className="display" style={{ fontSize: "1.1rem" }}>
          O que já está pronto x o que falta
        </span>
        <ul style={{ margin: 0, paddingLeft: 18, color: "var(--ink-dim)", fontSize: "0.9rem", display: "grid", gap: 6 }}>
          <li>
            <strong style={{ color: "var(--ink)" }}>Pronto:</strong> guardar as credenciais do app
            com segurança (segredo criptografado), testar a autenticação OAuth2 real com o
            iFood, mapear cada loja ao MerchantId correspondente, sincronizar pedidos (receber,
            confirmar dentro do SLA, iniciar preparo/pronto/cancelar), sincronizar cardápio
            (categorias, produtos, preço, pausar/reativar, estoque de produtos com controle de
            estoque), trilha financeira (lançamentos e repasses do iFood, alerta de
            discrepância), e operação da loja (status em tempo real, pausar/reabrir, horários de
            funcionamento, tempo de preparo customizado).
          </li>
          <li>
            <strong style={{ color: "var(--ink)" }}>Pendente:</strong> complementos/pizzas/combos e
            múltiplos canais no cardápio, logística com frota própria e pedidos externos com
            entrega Sob Demanda. Fora do escopo dos pedidos: rastreamento de entregador em tempo
            real fora da Fase 7, pedidos agendados, disputas pós-entrega (Handshake).
          </li>
        </ul>
      </section>
    </main>
  );
}

function MerchantMappingRow({
  mapping,
  onSaved,
}: {
  mapping: { branchId: number; branchName: string; merchantId: string | null; merchantUuid: string | null };
  onSaved: () => void;
}) {
  const toast = useToast();
  const [merchantId, setMerchantId] = useState(mapping.merchantId ?? "");
  const [merchantUuid, setMerchantUuid] = useState(mapping.merchantUuid ?? "");

  const mutation = useMutation({
    mutationFn: () =>
      setIFoodMerchantMapping({
        branchId: mapping.branchId,
        merchantId: merchantId.trim(),
        merchantUuid: merchantUuid.trim(),
      }),
    onSuccess: () => {
      toast.success(`Loja "${mapping.branchName}" atualizada.`);
      onSaved();
    },
    onError: () => toast.error("Não foi possível salvar essa loja."),
  });

  return (
    <div className="ui-row ui-row-wrap" style={{ alignItems: "end", gap: 10, borderTop: "1px solid var(--line-soft)", paddingTop: 12 }}>
      <div style={{ minWidth: 140, fontWeight: 600 }}>{mapping.branchName}</div>
      <div style={{ flex: 1, minWidth: 180 }}>
        <TextField
          label="Merchant ID"
          value={merchantId}
          onChange={(e) => setMerchantId(e.target.value)}
          placeholder="ID da loja no iFood"
        />
      </div>
      <div style={{ flex: 1, minWidth: 180 }}>
        <TextField
          label="Merchant UUID (opcional)"
          value={merchantUuid}
          onChange={(e) => setMerchantUuid(e.target.value)}
          placeholder="opcional"
        />
      </div>
      <Button variant="ghost" size="sm" loading={mutation.isPending} onClick={() => mutation.mutate()}>
        Salvar
      </Button>
    </div>
  );
}

const WEEKDAY_LABELS = ["Domingo", "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado"];

// Operação da loja iFood (fase 5, módulo Merchant) — status em tempo real, interrupções
// (pausar/reabrir), horários de funcionamento (cópia local editável, reenviada ao salvar) e
// tempo de preparo customizado. Tudo por filial — usa a mesma loja "padrão" (primeira mapeada)
// que a seção Financeiro, mesma decisão de não ter seletor de loja ainda.
function MerchantOperationsSection({ branchId }: { branchId: number }) {
  const queryClient = useQueryClient();
  const toast = useToast();

  const statusQuery = useQuery({
    queryKey: ["integrations", "ifood", "merchant", "status", branchId],
    queryFn: () => getIFoodMerchantStatus(branchId),
  });

  const interruptionsQuery = useQuery({
    queryKey: ["integrations", "ifood", "merchant", "interruptions", branchId],
    queryFn: () => getIFoodInterruptions(branchId),
  });

  const openingHoursQuery = useQuery({
    queryKey: ["integrations", "ifood", "merchant", "opening-hours", branchId],
    queryFn: () => getIFoodOpeningHours(branchId),
  });

  const [shifts, setShifts] = useState<IFoodOpeningHourShift[]>([]);
  const [prepTime, setPrepTime] = useState("");
  const [initializedBranchId, setInitializedBranchId] = useState<number | null>(null);

  useEffect(() => {
    if (openingHoursQuery.data && initializedBranchId !== branchId) {
      setShifts(openingHoursQuery.data.shifts);
      setPrepTime(openingHoursQuery.data.preparationTimeMinutes?.toString() ?? "");
      setInitializedBranchId(branchId);
    }
  }, [openingHoursQuery.data, branchId, initializedBranchId]);

  const [interruptionDescription, setInterruptionDescription] = useState("");
  const [interruptionStart, setInterruptionStart] = useState("");
  const [interruptionEnd, setInterruptionEnd] = useState("");

  const createInterruptionMutation = useMutation({
    mutationFn: () =>
      createIFoodInterruption({
        branchId,
        description: interruptionDescription.trim(),
        start: new Date(interruptionStart).toISOString(),
        end: new Date(interruptionEnd).toISOString(),
      }),
    onSuccess: () => {
      toast.success("Loja pausada no iFood.");
      setInterruptionDescription("");
      setInterruptionStart("");
      setInterruptionEnd("");
      void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "merchant", "interruptions", branchId] });
    },
    onError: () => toast.error("Não foi possível pausar a loja."),
  });

  const deleteInterruptionMutation = useMutation({
    mutationFn: (interruptionId: string) => deleteIFoodInterruption(branchId, interruptionId),
    onSuccess: () => {
      toast.success("Loja reaberta.");
      void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "merchant", "interruptions", branchId] });
    },
    onError: () => toast.error("Não foi possível reabrir a loja."),
  });

  const saveOpeningHoursMutation = useMutation({
    mutationFn: () => saveIFoodOpeningHours(branchId, shifts),
    onSuccess: () => {
      toast.success("Horários de funcionamento salvos e enviados ao iFood.");
      void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "merchant", "opening-hours", branchId] });
    },
    onError: () => toast.error("Não foi possível salvar os horários — confira os turnos e tente de novo."),
  });

  const setPrepTimeMutation = useMutation({
    mutationFn: () => setIFoodPreparationTime(branchId, prepTime.trim() === "" ? null : Number(prepTime)),
    onSuccess: () => {
      toast.success("Tempo de preparo atualizado.");
      void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "merchant", "opening-hours", branchId] });
    },
    onError: () => toast.error("Não foi possível atualizar o tempo de preparo."),
  });

  const addShift = () => setShifts((prev) => [...prev, { dayOfWeek: 1, start: "08:00", durationMinutes: 600 }]);
  const removeShift = (index: number) => setShifts((prev) => prev.filter((_, i) => i !== index));
  const updateShift = (index: number, patch: Partial<IFoodOpeningHourShift>) =>
    setShifts((prev) => prev.map((s, i) => (i === index ? { ...s, ...patch } : s)));

  const hasIFoodCustomerId = openingHoursQuery.data?.hasIFoodCustomerId ?? false;

  return (
    <section className="ticket rise rise-3" style={{ padding: 20, display: "grid", gap: 20, marginTop: 16 }}>
      <div style={{ display: "grid", gap: 4 }}>
        <span className="display" style={{ fontSize: "1.2rem" }}>
          Operação da loja
        </span>
        <span style={{ color: "var(--ink-dim)", fontSize: "0.92rem" }}>
          Status em tempo real, pausas/reaberturas, horários de funcionamento e tempo de preparo
          customizado — tudo direto pelo módulo Merchant do iFood.
        </span>
      </div>

      {/* Status */}
      <div style={{ display: "grid", gap: 8, borderTop: "1px solid var(--line-soft)", paddingTop: 14 }}>
        <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", alignItems: "center", gap: 12 }}>
          <span style={{ fontWeight: 600 }}>Status</span>
          <Button variant="ghost" size="sm" loading={statusQuery.isFetching} onClick={() => void statusQuery.refetch()}>
            Atualizar status
          </Button>
        </div>
        {statusQuery.isError && <QueryError error={statusQuery.error} what="o status da loja" />}
        {statusQuery.data && (
          <div style={{ display: "grid", gap: 6 }}>
            <span className="chip">{statusQuery.data.operationState ?? "Desconhecido"}</span>
            {statusQuery.data.validations.map((v) => (
              <span key={v.id} style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
                {v.id} ({v.state}){v.message ? ` — ${v.message}` : ""}
              </span>
            ))}
          </div>
        )}
      </div>

      {/* Interrupções */}
      <div style={{ display: "grid", gap: 10, borderTop: "1px solid var(--line-soft)", paddingTop: 14 }}>
        <span style={{ fontWeight: 600 }}>Interrupções (pausar/reabrir)</span>
        {interruptionsQuery.isError && <QueryError error={interruptionsQuery.error} what="as interrupções" />}
        {(interruptionsQuery.data ?? []).length === 0 && !interruptionsQuery.isLoading && (
          <span style={{ color: "var(--ink-faint)", fontSize: "0.86rem" }}>Nenhuma pausa ativa.</span>
        )}
        {(interruptionsQuery.data ?? []).map((i) => (
          <div key={i.id} className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", gap: 10, fontSize: "0.88rem" }}>
            <span>
              {i.description ?? "(sem motivo)"} — {new Date(i.start).toLocaleString("pt-BR")} até{" "}
              {new Date(i.end).toLocaleString("pt-BR")}
            </span>
            <Button
              variant="ghost"
              size="sm"
              loading={deleteInterruptionMutation.isPending}
              onClick={() => deleteInterruptionMutation.mutate(i.id)}
            >
              Reabrir
            </Button>
          </div>
        ))}

        <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "end" }}>
          <div style={{ flex: 2, minWidth: 160 }}>
            <TextField
              label="Motivo"
              value={interruptionDescription}
              onChange={(e) => setInterruptionDescription(e.target.value)}
              placeholder="ex.: falta de insumo"
            />
          </div>
          <div style={{ flex: 1, minWidth: 160 }}>
            <TextField
              label="Início"
              type="datetime-local"
              value={interruptionStart}
              onChange={(e) => setInterruptionStart(e.target.value)}
            />
          </div>
          <div style={{ flex: 1, minWidth: 160 }}>
            <TextField
              label="Fim"
              type="datetime-local"
              value={interruptionEnd}
              onChange={(e) => setInterruptionEnd(e.target.value)}
            />
          </div>
          <Button
            variant="ghost"
            size="sm"
            loading={createInterruptionMutation.isPending}
            disabled={!interruptionDescription.trim() || !interruptionStart || !interruptionEnd}
            onClick={() => createInterruptionMutation.mutate()}
          >
            Pausar loja
          </Button>
        </div>
      </div>

      {/* Horários de funcionamento */}
      <div style={{ display: "grid", gap: 10, borderTop: "1px solid var(--line-soft)", paddingTop: 14 }}>
        <span style={{ fontWeight: 600 }}>Horários de funcionamento</span>
        <span style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
          Ao salvar, a lista inteira de turnos é reenviada ao iFood (substitui tudo, não é
          incremental).
        </span>
        {shifts.map((shift, index) => (
          <div key={index} className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "end" }}>
            <div style={{ minWidth: 140 }}>
              <SelectField
                label="Dia da semana"
                value={shift.dayOfWeek}
                onChange={(e) => updateShift(index, { dayOfWeek: Number(e.target.value) })}
              >
                {WEEKDAY_LABELS.map((label, day) => (
                  <option key={day} value={day}>
                    {label}
                  </option>
                ))}
              </SelectField>
            </div>
            <div style={{ minWidth: 120 }}>
              <TextField
                label="Início"
                type="time"
                value={shift.start}
                onChange={(e) => updateShift(index, { start: e.target.value })}
              />
            </div>
            <div style={{ minWidth: 120 }}>
              <TextField
                label="Duração (min)"
                type="number"
                value={shift.durationMinutes.toString()}
                onChange={(e) => updateShift(index, { durationMinutes: Number(e.target.value) })}
              />
            </div>
            <Button variant="ghost" size="sm" onClick={() => removeShift(index)}>
              Remover
            </Button>
          </div>
        ))}
        <div className="ui-row" style={{ gap: 10 }}>
          <Button variant="ghost" size="sm" onClick={addShift}>
            + Adicionar turno
          </Button>
          <Button
            variant="primary"
            size="sm"
            loading={saveOpeningHoursMutation.isPending}
            onClick={() => saveOpeningHoursMutation.mutate()}
          >
            Salvar horários
          </Button>
        </div>
      </div>

      {/* Tempo de preparo */}
      <div style={{ display: "grid", gap: 10, borderTop: "1px solid var(--line-soft)", paddingTop: 14 }}>
        <span style={{ fontWeight: 600 }}>Tempo de preparo</span>
        {!hasIFoodCustomerId && (
          <span style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
            Configure o "iFood Customer ID" na seção de credenciais acima para habilitar este
            campo.
          </span>
        )}
        <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "end" }}>
          <div style={{ minWidth: 160 }}>
            <TextField
              label="Minutos (vazio = automático do iFood)"
              type="number"
              value={prepTime}
              onChange={(e) => setPrepTime(e.target.value)}
              disabled={!hasIFoodCustomerId}
              placeholder="ex.: 30"
            />
          </div>
          <Button
            variant="primary"
            size="sm"
            loading={setPrepTimeMutation.isPending}
            disabled={!hasIFoodCustomerId}
            onClick={() => setPrepTimeMutation.mutate()}
          >
            Salvar
          </Button>
        </div>
      </div>
    </section>
  );
}
