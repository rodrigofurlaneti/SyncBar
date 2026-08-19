import { useEffect, useState, type CSSProperties } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  getIFoodMerchantMappings,
  getIFoodSettings,
  saveIFoodSettings,
  setIFoodMerchantMapping,
  testIFoodConnection,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { useToast } from "../../ui/Toast";
import { Field, TextField } from "../../ui/Field";
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

      <section className="ticket rise rise-3" style={{ padding: 20, display: "grid", gap: 8, marginTop: 16 }}>
        <span className="display" style={{ fontSize: "1.1rem" }}>
          O que já está pronto x o que falta
        </span>
        <ul style={{ margin: 0, paddingLeft: 18, color: "var(--ink-dim)", fontSize: "0.9rem", display: "grid", gap: 6 }}>
          <li>
            <strong style={{ color: "var(--ink)" }}>Pronto:</strong> guardar as credenciais do app
            com segurança (segredo criptografado), testar a autenticação OAuth2 real com o
            iFood, mapear cada loja ao MerchantId correspondente, e sincronizar pedidos (receber,
            confirmar dentro do SLA, iniciar preparo/pronto/cancelar).
          </li>
          <li>
            <strong style={{ color: "var(--ink)" }}>Pendente:</strong> cardápio e financeiro —
            dependem de confirmar os endpoints e formatos exatos nos módulos
            "Catalog"/"Financial" da documentação oficial. Também fora do escopo desta fase:
            rastreamento de entregador, pedidos agendados, disputas pós-entrega (Handshake), e
            despacho com frota própria (hoje todo pedido pronto usa "retirada/coleta").
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
