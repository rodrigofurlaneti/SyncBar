import { useState, type CSSProperties } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getServiceFeeSetting, setSelfServiceEmployee, setServiceFeeEnabled, getQrViewSetting, setQrViewEnabled } from "./api";
import { getComandaSetting, setComandaDefaultLimit } from "../comandas/api";
import { getEmployeesByBranch } from "../employees/api";
import { useAuthStore } from "../../stores/authStore";
import { useToast } from "../../ui/Toast";
import { Switch } from "../../ui/Switch";
import { formatBRL } from "../../lib/types";
import { QueryError } from "../../components/QueryError";

const cards = [
    { to: "/", label: "Pedidos", desc: "Controle de mesas e comandas" },
    { to: "/delivery", label: "Delivery", desc: "Painel de entregas e expedição" },
    { to: "/preparo", label: "Preparo (KDS)", desc: "Monitor de cozinha e produção" },
    { to: "/garcom", label: "Visão do Garçom", desc: "Interface de atendimento no salão" },
    { to: "/pracas", label: "Praças e Salões", desc: "Gestão de áreas, mesas e turnos de garçons" },
    { to: "/produtos", label: "Produtos", desc: "Catálogo e preços do cardápio" },
    { to: "/complementos", label: "Complementos", desc: "Adicionais, bordas e variações" },
    { to: "/estoque", label: "Estoque", desc: "Controle de inventário e insumos" },
    { to: "/compras", label: "Compras", desc: "Fornecedores e entrada de estoque" },
    { to: "/clientes", label: "Clientes", desc: "Cadastro e fidelidade" },
    { to: "/reservas", label: "Reservas", desc: "Agenda de reservas de mesa" },
    { to: "/equipe", label: "Equipe", desc: "Funcionários e cargos" },
    { to: "/usuarios", label: "Usuários", desc: "Contas de acesso ao sistema" },
    { to: "/acessos", label: "Acessos", desc: "Papéis e permissões dos usuários" },
    { to: "/faturamento", label: "Faturamento", desc: "Custos e metas do mês" },
    { to: "/fechamentos", label: "Fechamentos", desc: "Histórico de sessões de caixa" },
    { to: "/relatorios", label: "Relatórios", desc: "Vendas, produtos e taxa de serviço" },
    { to: "/cenarios", label: "Cenários", desc: "Projeções e simulações" },
    { to: "/promocoes", label: "Promoções", desc: "Ofertas e descontos ativos" },
    { to: "/impressao", label: "Impressão", desc: "Impressoras e cupons" },
    { to: "/integracoes/ifood", label: "Integração iFood", desc: "Credenciais e conexão com o iFood" },
];

export function SettingsPage() {
    const queryClient = useQueryClient();
    const toast = useToast();
    const { branchId } = useAuthStore();
    const [limitInput, setLimitInput] = useState("");
    const [selfServiceEmployeeId, setSelfServiceEmployeeId] = useState("");

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

    const qrViewQuery = useQuery({
        queryKey: ["orders", "qr-view-setting", branchId],
        queryFn: () => getQrViewSetting(branchId),
    });
    const qrViewEnabled = qrViewQuery.data?.enabled ?? true;

    const qrViewMutation = useMutation({
        mutationFn: (next: boolean) => setQrViewEnabled(branchId, next),
        onSuccess: (_data, next) => {
            toast.success(next ? "Visualização do cliente LIGADA." : "Visualização do cliente DESLIGADA.");
            void queryClient.invalidateQueries({ queryKey: ["orders", "qr-view-setting"] });
        },
        onError: () => toast.error("Não foi possível alterar a visualização."),
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

    const employeesQuery = useQuery({
        queryKey: ["employees", branchId],
        queryFn: () => getEmployeesByBranch(branchId),
    });

    const selfServiceMutation = useMutation({
        mutationFn: (employeeId: number | null) => setSelfServiceEmployee(branchId, employeeId),
        onSuccess: () => toast.success("Funcionário de autoatendimento atualizado."),
        onError: () => toast.error("Não foi possível salvar — confirme que você é administrador."),
    });

    return (
        <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }}>
            <div className="rise" style={{ marginBottom: 18 }}>
                <h2 className="display" style={{ fontSize: "1.7rem" }}>Configurações</h2>
                <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
                    gestão da filial — somente gerente/administrador
                </span>
            </div>

            {(feeQuery.isError || qrViewQuery.isError) && (
                <QueryError error={feeQuery.error || qrViewQuery.error} what="as configurações" />
            )}

            <section className="ticket rise rise-1" style={{ padding: 20, display: "grid", gap: 18 }}>
                <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", gap: 16 }}>
                    <div style={{ display: "grid", gap: 4, maxWidth: 520 }}>
                        <span className="display" style={{ fontSize: "1.2rem" }}>Taxa de serviço (10%)</span>
                        <span style={{ color: "var(--ink-dim)", fontSize: "0.92rem" }}>
                            Quando desligada, as contas fechadas nesta filial não cobram os 10% e a taxa
                            nem aparece na conta impressa. Ideal para eventos sem taxa — vale para o dia inteiro.
                        </span>
                    </div>
                    <div className="ui-row" style={{ gap: 12 }}>
                        <span className="chip" style={{ "--dot": feeEnabled ? "var(--ok)" : "var(--ink-faint)" } as CSSProperties}>
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
                        <span className="display" style={{ fontSize: "1.2rem" }}>Visualização do cliente (QR Code)</span>
                        <span style={{ color: "var(--ink-dim)", fontSize: "0.92rem" }}>
                            Cliente pode consultar o valor total da comanda e os pedidos lançados via QR Code na mesa.
                        </span>
                    </div>
                    <div className="ui-row" style={{ gap: 12 }}>
                        <span className="chip" style={{ "--dot": qrViewEnabled ? "var(--ok)" : "var(--ink-faint)" } as CSSProperties}>
                            {qrViewEnabled ? "Ligada" : "Desligada"}
                        </span>
                        <Switch
                            checked={qrViewEnabled}
                            disabled={qrViewQuery.isLoading || qrViewMutation.isPending}
                            onChange={(next) => qrViewMutation.mutate(next)}
                            label="Aparece a opção de mesa ou comanda"
                        />
                    </div>
                </div>

                <div style={{ borderTop: "1px solid var(--line-soft)" }} />

                <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", gap: 16 }}>
                    <div style={{ display: "grid", gap: 4, maxWidth: 520 }}>
                        <span className="display" style={{ fontSize: "1.2rem" }}>Limite de comanda</span>
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
                            type="button"
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

                <div style={{ borderTop: "1px solid var(--line-soft)" }} />

                <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", gap: 16 }}>
                    <div style={{ display: "grid", gap: 4, maxWidth: 520 }}>
                        <span className="display" style={{ fontSize: "1.2rem" }}>Autoatendimento (QR Code)</span>
                        <span style={{ color: "var(--ink-dim)", fontSize: "0.92rem" }}>
                            Funcionário que "abre" os pedidos lançados por clientes via QR Code na mesa.
                            Obrigatório configurar antes de gerar QR Codes em Salão.
                        </span>
                    </div>
                    <div className="ui-row" style={{ gap: 8 }}>
                        <select
                            value={selfServiceEmployeeId}
                            onChange={(e) => setSelfServiceEmployeeId(e.target.value)}
                            style={{ width: 200 }}
                        >
                            <option value="">Selecione…</option>
                            {(employeesQuery.data ?? []).filter((e) => e.isActive).map((e) => (
                                <option key={e.id} value={e.id}>{e.name}</option>
                            ))}
                        </select>
                        <button
                            type="button"
                            className="btn-ghost"
                            disabled={selfServiceEmployeeId === "" || selfServiceMutation.isPending}
                            onClick={() => selfServiceMutation.mutate(Number(selfServiceEmployeeId))}
                        >
                            Salvar
                        </button>
                    </div>
                </div>
            </section>

            <h3 className="display rise rise-2" style={{ fontSize: "1.2rem", margin: "26px 0 12px" }}>
                Navegação do Sistema
            </h3>
            <div className="rise rise-2" style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(230px, 1fr))", gap: 12 }}>
                {cards.map((card) => (
                    <Link key={card.to} to={card.to} className="ticket" style={{ padding: "16px 18px", display: "grid", gap: 4, textDecoration: "none", minHeight: 84 }}>
                        <span className="display" style={{ fontSize: "1.05rem", color: "var(--ink)" }}>{card.label}</span>
                        <span style={{ color: "var(--ink-dim)", fontSize: "0.86rem" }}>{card.desc}</span>
                    </Link>
                ))}
            </div>
        </main>
    );
}