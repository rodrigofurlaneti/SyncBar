import { useState, type CSSProperties } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getServiceFeeSetting, setServiceFeeEnabled } from "./api";
import { getComandaSetting, setComandaDefaultLimit } from "../comandas/api";
import { useAuthStore } from "../../stores/authStore";
import { useToast } from "../../ui/Toast";
import { Switch } from "../../ui/Switch";
import { formatBRL } from "../../lib/types";
import { QueryError } from "../../components/QueryError";

const cards = [
  { to: "/acessos", label: "Acessos", desc: "Papéis e permissões dos usuários" },
  { to: "/usuarios", label: "Usuários", desc: "Contas de acesso ao sistema" },
  { to: "/equipe", label: "Equipe", desc: "Funcionários e cargos" },
  { to: "/faturamento", label: "Faturamento", desc: "Custos e metas do mês" },
  { to: "/relatorios", label: "Relatórios", desc: "Vendas, produtos e taxa de serviço" },
  { to: "/cenarios", label: "Cenários", desc: "Projeções e simulações" },
  { to: "/fechamentos", label: "Fechamentos", desc: "Histórico de sessões de caixa" },
  { to: "/promocoes", label: "Promoções", desc: "Ofertas e descontos ativos" },
  { to: "/impressao", label: "Impressão", desc: "Impressoras e cupons" },
];

export function SettingsPage() {
  const queryClient = useQueryClient();
  const toast = useToast();
  const { branchId } = useAuthStore();
  const [limitInput, setLimitInput] = useState("");

  const feeQuery = useQuery({
    queryKey: ["orders", "service-fee-setting", branchId],
    queryFn: () => getServiceFeeSetting(branchId),
  });
  const feeEnabled = feeQuery.data?.enabled ?? true;

  const feeMutation = useMutation({
    mutationFn: (next: boolean) => setServiceFeeEnabled(branchId, next),
    onSuccess: (_data, next) => {
      toast.success(next ? "Taxa de serviço (10%) LIGADA." : "Taxa de serviço (10%) DESLIGADA.");
      void queryClient.invalidateQueries({ queryKey: ["orders", "service-fee-setting"] });
    },
    onError: () => toast.error("Não foi possível alterar a taxa de serviço."),
  });

  const comandaQuery = useQuery({
    queryKey: ["comandas", "setting", branchId],
    queryFn: () => getComandaSetting(branchId),
  });

  const limitMutation = useMutation({
    mutationFn: (value: number) => setComandaDefaultLimit(branchId, value),
    onSuccess: () => {
      setLimitInput("");
      toast.success("Limite de comanda atualizado.");
      void queryClient.invalidateQueries({ queryKey: ["comandas", "setting"] });
    },
    onError: () => toast.error("Não foi possível salvar o limite."),
  });

  return (
    <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }}>
      <div className="rise" style={{ marginBottom: 18 }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>
          Configurações
        </h2>
        <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
          gestão da filial — somente gerente/administrador
        </span>
      </div>

      {feeQuery.isError && <QueryError error={feeQuery.error} what="as configurações" />}

      {/* Ajustes rápidos da filial */}
      <section className="ticket rise rise-1" style={{ padding: 20, display: "grid", gap: 18 }}>
        <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", gap: 16 }}>
          <div style={{ display: "grid", gap: 4, maxWidth: 520 }}>
            <span className="display" style={{ fontSize: "1.2rem" }}>
              Taxa de serviço (10%)
            </span>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.92rem" }}>
              Quando desligada, as contas fechadas nesta filial não cobram os 10% e a taxa
              nem aparece na conta impressa. Ideal para eventos sem taxa — vale para o dia inteiro.
            </span>
          </div>
          <div className="ui-row" style={{ gap: 12 }}>
            <span
              className="chip"
              style={{ "--dot": feeEnabled ? "var(--ok)" : "var(--ink-faint)" } as CSSProperties}
            >
              {feeEnabled ? "Ligada" : "Desligada"}
            </span>
            <Switch
              checked={feeEnabled}
              disabled={feeQuery.isLoading || feeMutation.isPending}
              onChange={(next) => feeMutation.mutate(next)}
              label="Cobrar taxa de serviço de 10%"
            />
          </div>
        </div>

        <div style={{ borderTop: "1px solid var(--line-soft)" }} />

        <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", gap: 16 }}>
          <div style={{ display: "grid", gap: 4, maxWidth: 520 }}>
            <span className="display" style={{ fontSize: "1.2rem" }}>
              Limite de comanda
            </span>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.92rem" }}>
              Valor máximo de consumo padrão para novas comandas desta filial.
            </span>
          </div>
          <div className="ui-row" style={{ gap: 8 }}>
            {comandaQuery.data && (
              <span className="chip" style={{ "--dot": "var(--busy)" } as CSSProperties}>
                atual {formatBRL(comandaQuery.data.defaultLimitAmount)}
              </span>
            )}
            <input
              placeholder="novo limite"
              inputMode="decimal"
              value={limitInput}
              onChange={(e) => setLimitInput(e.target.value)}
              style={{ width: 130 }}
            />
            <button
              className="btn-ghost"
              disabled={limitMutation.isPending || limitInput.trim() === ""}
              onClick={() => {
                const value = Number(limitInput.replace(",", "."));
                if (Number.isFinite(value) && value > 0) limitMutation.mutate(value);
              }}
            >
              Salvar
            </button>
          </div>
        </div>
      </section>

      {/* Gestão — atalhos para as telas administrativas */}
      <h3 className="display rise rise-2" style={{ fontSize: "1.2rem", margin: "26px 0 12px" }}>
        Gestão
      </h3>
      <div
        className="rise rise-2"
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fill, minmax(230px, 1fr))",
          gap: 12,
        }}
      >
        {cards.map((card) => (
          <Link
            key={card.to}
            to={card.to}
            className="ticket"
            style={{
              padding: "16px 18px",
              display: "grid",
              gap: 4,
              textDecoration: "none",
              minHeight: 84,
            }}
          >
            <span className="display" style={{ fontSize: "1.05rem", color: "var(--ink)" }}>
              {card.label}
            </span>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.86rem" }}>{card.desc}</span>
          </Link>
        ))}
      </div>
    </main>
  );
}
