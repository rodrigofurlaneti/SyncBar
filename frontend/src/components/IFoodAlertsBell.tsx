import { useEffect, useRef, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  acknowledgeIFoodOperationalAlert,
  getIFoodOperationalAlerts,
  type IFoodOperationalAlert,
} from "../features/integrations/api";
import { useToast } from "../ui/Toast";

const POLL_INTERVAL_MS = 30_000;

const SEVERITY_COLOR: Record<IFoodOperationalAlert["severity"], string> = {
  Info: "var(--info, #3b82f6)",
  Warning: "var(--warn, #d97706)",
  Critical: "var(--danger, #dc2626)",
};

const SEVERITY_ICON: Record<IFoodOperationalAlert["severity"], string> = {
  Info: "🔵",
  Warning: "🟠",
  Critical: "🔴",
};

// Mesma técnica de aviso sonoro da Fase 12 (IFoodOrdersPage — pedido novo), mas com um único tom
// grave pra não ser confundido com "chegou pedido novo": alerta operacional (loja caiu do iFood)
// é outra categoria de urgência. Sem depender de arquivo de áudio externo; falha silenciosamente
// se o navegador bloquear áudio antes de qualquer interação do usuário — o toast visual continua
// avisando de qualquer forma.
function playAlertChime() {
  try {
    const AudioCtxCtor =
      window.AudioContext ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!AudioCtxCtor) return;
    const ctx = new AudioCtxCtor();
    const oscillator = ctx.createOscillator();
    const gain = ctx.createGain();
    oscillator.type = "square";
    oscillator.frequency.value = 440;
    const now = ctx.currentTime;
    gain.gain.setValueAtTime(0.0001, now);
    gain.gain.exponentialRampToValueAtTime(0.25, now + 0.02);
    gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.5);
    oscillator.connect(gain);
    gain.connect(ctx.destination);
    oscillator.start(now);
    oscillator.stop(now + 0.5);
    setTimeout(() => void ctx.close(), 900);
  } catch {
    // Ambiente sem suporte a Web Audio, ou áudio bloqueado — segue só com o toast visual.
  }
}

/// <summary>
/// Sino de alertas operacionais do iFood (Fase 13). Hoje só recebe os avisos do watcher de
/// status de loja (IFoodMerchantStatusWatcherBackgroundService — loja caiu/voltou no iFood), mas
/// a lista vem de um endpoint genérico (GET ifood/alerts/company/{id}) que qualquer outro job de
/// fundo do módulo iFood pode alimentar no futuro sem exigir mudança nesta tela. Fica no topo,
/// ao lado dos outros botões operacionais, e só aparece pra quem já enxerga Config./iFood
/// (mesmo gate de acesso — administra a empresa).
/// </summary>
export function IFoodAlertsBell({ companyId }: { companyId: number | null }) {
  const queryClient = useQueryClient();
  const toast = useToast();
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const knownAlertIdsRef = useRef<Set<string> | null>(null);
  const lastCompanyIdRef = useRef<number | null>(null);

  const alertsQuery = useQuery({
    queryKey: ["integrations", "ifood", "alerts", companyId],
    queryFn: () => getIFoodOperationalAlerts(companyId as number),
    enabled: companyId != null,
    refetchInterval: POLL_INTERVAL_MS,
  });

  const alerts = alertsQuery.data ?? [];

  // Mesmo padrão de detecção de "novidade" da Fase 12 (pedido novo): compara contra o snapshot
  // anterior de IDs, e o primeiro carregamento só grava a baseline sem soar alarme — senão todo
  // alerta que já estava pendente antes de abrir a tela viraria um "alerta novo" barulhento.
  useEffect(() => {
    if (!alertsQuery.data) return;

    const currentIds = new Set(alertsQuery.data.map((a) => a.id));

    // Correção pós-revisão (CodeRabbit, PR #4): reseta a baseline quando a empresa selecionada
    // muda. Sem isso, o primeiro carregamento de alertas da empresa NOVA comparava contra o
    // snapshot de IDs da empresa ANTERIOR — todo alerta que já existia (só que de outra empresa,
    // nunca visto por este componente) virava um "alerta novo" barulhento (bipe + toast).
    if (lastCompanyIdRef.current !== companyId) {
      lastCompanyIdRef.current = companyId;
      knownAlertIdsRef.current = currentIds;
      return;
    }

    if (knownAlertIdsRef.current === null) {
      knownAlertIdsRef.current = currentIds;
      return;
    }

    const previouslyKnown = knownAlertIdsRef.current;
    const newAlerts = alertsQuery.data.filter((a) => !previouslyKnown.has(a.id));
    knownAlertIdsRef.current = currentIds;

    if (newAlerts.length === 0) return;

    playAlertChime();
    for (const alert of newAlerts) {
      const icon = SEVERITY_ICON[alert.severity];
      toast.info(`${icon} ${alert.title} — ${alert.branchName}`);
    }
  }, [alertsQuery.data, toast, companyId]);

  // Fecha o dropdown ao clicar fora dele.
  useEffect(() => {
    if (!open) return;
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [open]);

  if (companyId == null) return null;

  const handleAcknowledge = async (alertId: string) => {
    try {
      await acknowledgeIFoodOperationalAlert(companyId, alertId);
      queryClient.setQueryData<IFoodOperationalAlert[]>(
        ["integrations", "ifood", "alerts", companyId],
        (previous) => (previous ?? []).filter((a) => a.id !== alertId),
      );
    } catch {
      toast.error("Não foi possível marcar o alerta como visto.");
    }
  };

  return (
    <div ref={containerRef} style={{ position: "relative" }}>
      <button
        type="button"
        className="btn-ghost btn-icon"
        aria-label={alerts.length > 0 ? `${alerts.length} alertas do iFood` : "Alertas do iFood"}
        title="Alertas operacionais do iFood"
        onClick={() => setOpen((v) => !v)}
        style={{ position: "relative" }}
      >
        🔔
        {alerts.length > 0 && (
          <span
            style={{
              position: "absolute",
              top: -4,
              right: -4,
              background: "var(--danger, #dc2626)",
              color: "#fff",
              borderRadius: "999px",
              fontSize: "0.65rem",
              fontWeight: 700,
              lineHeight: 1,
              padding: "3px 5px",
              minWidth: 16,
              textAlign: "center",
            }}
          >
            {alerts.length > 9 ? "9+" : alerts.length}
          </span>
        )}
      </button>

      {open && (
        <div
          style={{
            position: "absolute",
            top: "calc(100% + 8px)",
            right: 0,
            width: 340,
            maxHeight: 420,
            overflowY: "auto",
            background: "var(--surface, var(--panel-bg, #1e1e1e))",
            border: "1px solid var(--border, #d0d0d0)",
            borderRadius: 10,
            boxShadow: "0 8px 24px rgba(0,0,0,0.18)",
            zIndex: 1000,
            padding: alerts.length === 0 ? 16 : 6,
          }}
        >
          {alerts.length === 0 ? (
            <p style={{ margin: 0, color: "var(--ink-dim)", fontSize: "0.9rem" }}>
              Nenhum alerta operacional do iFood no momento.
            </p>
          ) : (
            alerts.map((alert) => (
              <div
                key={alert.id}
                style={{
                  padding: "10px 10px",
                  borderLeft: `3px solid ${SEVERITY_COLOR[alert.severity]}`,
                  borderRadius: 6,
                  marginBottom: 4,
                  background: "var(--surface-raised, rgba(0,0,0,0.03))",
                }}
              >
                <div style={{ display: "flex", justifyContent: "space-between", gap: 8 }}>
                  <strong style={{ fontSize: "0.85rem" }}>{alert.title}</strong>
                  <button
                    type="button"
                    className="btn-ghost"
                    style={{ padding: "2px 8px", fontSize: "0.75rem" }}
                    onClick={() => void handleAcknowledge(alert.id)}
                  >
                    OK
                  </button>
                </div>
                <p style={{ margin: "4px 0 0", fontSize: "0.8rem", color: "var(--ink-dim)" }}>{alert.message}</p>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
}
