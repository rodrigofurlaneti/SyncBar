import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Swal from "sweetalert2"; // Adicionado SweetAlert2
import { createPromotion, deactivatePromotion, getPromotionsByBranch } from "./api";
import { getMenu } from "../catalog/api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { PromotionType, dayOfWeekLabel, hhmmToMinutes, minutesToHHmm, promotionBadge } from "../../lib/types";
import { QueryError } from "../../components/QueryError";
import { Overlay } from "../orders/Overlay";

// Configuração do Toast do SweetAlert2
const Toast = Swal.mixin({
    toast: true,
    position: "top-end",
    showConfirmButton: false,
    timer: 3000,
    timerProgressBar: true,
});

export function PromotionsPage() {
    const queryClient = useQueryClient();
    const { branchId, companyId } = useAuthStore();
    const [creating, setCreating] = useState(false);
    const [productId, setProductId] = useState("");
    const [name, setName] = useState("");
    const [dayOfWeek, setDayOfWeek] = useState(3);
    const [startTime, setStartTime] = useState("16:00");
    const [endTime, setEndTime] = useState("20:00");
    const [typeId, setTypeId] = useState<number>(PromotionType.EmDobro);
    const [discountPct, setDiscountPct] = useState("25");
    const [error, setError] = useState<string | null>(null);

    const promotionsQuery = useQuery({
        queryKey: ["promotions", branchId],
        queryFn: () => getPromotionsByBranch(branchId),
    });

    const menuQuery = useQuery({
        queryKey: ["menu", companyId],
        queryFn: () => getMenu(companyId ?? 1),
    });

    const productName = useMemo(() => {
        const map = new Map<number, string>();
        for (const p of menuQuery.data ?? []) map.set(p.id, p.name);
        return map;
    }, [menuQuery.data]);

    const byDay = useMemo(() => {
        const groups = new Map<number, typeof promotionsQuery.data>();
        for (const promo of promotionsQuery.data ?? []) {
            const list = groups.get(promo.dayOfWeek) ?? [];
            groups.set(promo.dayOfWeek, [...(list ?? []), promo]);
        }
        return groups;
    }, [promotionsQuery.data]);

    const refresh = () => void queryClient.invalidateQueries({ queryKey: ["promotions"] });

    const onApiError = (e: unknown) => {
        const msg = e instanceof ApiError ? e.message : "Operação falhou.";
        setError(msg);
    };

    const startMin = hhmmToMinutes(startTime);
    const endMin = hhmmToMinutes(endTime);
    const windowValid = startMin !== null && endMin !== null && startMin < endMin;
    const pct = Number(discountPct.replace(",", "."));
    const discountValid = typeId !== PromotionType.Desconto || (Number.isFinite(pct) && pct > 0 && pct < 100);

    const createMutation = useMutation({
        mutationFn: () =>
            createPromotion({
                branchId,
                productId: Number(productId),
                name: name.trim(),
                dayOfWeek,
                startMinuteOfDay: startMin ?? 0,
                endMinuteOfDay: endMin ?? 0,
                promotionTypeId: typeId,
                discountRate: typeId === PromotionType.Desconto ? pct / 100 : null,
            }),
        onSuccess: () => {
            setError(null);
            setCreating(false);
            setName("");
            setProductId("");
            Toast.fire({ icon: "success", title: "Promoção criada com sucesso." });
            refresh();
        },
        onError: onApiError,
    });

    const removeMutation = useMutation({
        mutationFn: (id: number) => deactivatePromotion(id),
        onSuccess: () => {
            Toast.fire({ icon: "success", title: "Promoção encerrada." });
            refresh();
        },
        onError: (e) => {
            Swal.fire("Erro", e instanceof ApiError ? e.message : "Falha ao encerrar a promoção.", "error");
        },
    });

    return (
        <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }} data-testid="promotions-page-main">
            <div className="rise" style={{ display: "flex", alignItems: "baseline", gap: 14, marginBottom: 6, flexWrap: "wrap" }}>
                <h2 className="display" style={{ fontSize: "1.7rem" }}>Promoções</h2>
                <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
                    em dobro: o cliente paga 1 e leva 2 — vale só para pedidos dentro da janela
                </span>
                <span style={{ flex: 1 }} />
                <button
                    type="button"
                    className="btn-primary"
                    onClick={() => { setError(null); setCreating(true); }}
                    data-testid="btn-new-promo"
                >
                    + Nova promoção
                </button>
            </div>

            {promotionsQuery.isError && <QueryError error={promotionsQuery.error} what="as promoções" />}
            {error && !creating && <p className="error-text" data-testid="main-error-msg">{error}</p>}

            <div className="rise rise-1" style={{ display: "grid", gap: 12, marginTop: 12 }}>
                {[1, 2, 3, 4, 5, 6, 0].map((day) => {
                    const promos = byDay.get(day) ?? [];
                    if (promos.length === 0) return null;
                    return (
                        <div className="ticket" key={day} data-testid={`promo-group-day-${day}`}>
                            <div className="ticket-head">
                                <span className="display" style={{ fontSize: "1.2rem", color: "var(--amber)" }}>
                                    {dayOfWeekLabel[day]}
                                </span>
                            </div>
                            {promos.map((promo) => (
                                <div className="ticket-row" key={promo.id} data-testid={`promo-row-${promo.id}`}>
                                    <div style={{ display: "grid", gap: 2 }}>
                                        <span>
                                            {promo.name}
                                            <span
                                                style={{
                                                    marginLeft: 8,
                                                    fontFamily: "var(--font-cond)",
                                                    fontSize: "0.68rem",
                                                    letterSpacing: "0.08em",
                                                    color: "var(--amber-ink)",
                                                    background: "var(--amber)",
                                                    borderRadius: 4,
                                                    padding: "2px 6px",
                                                    fontWeight: 700,
                                                }}
                                                data-testid={`promo-badge-${promo.id}`}
                                            >
                                                {promotionBadge(promo)}
                                            </span>
                                        </span>
                                        <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                                            {productName.get(promo.productId) ?? `Produto ${promo.productId}`} · das{" "}
                                            <span className="mono-num">{minutesToHHmm(promo.startMinuteOfDay)}</span> às{" "}
                                            <span className="mono-num">{minutesToHHmm(promo.endMinuteOfDay)}</span>
                                        </span>
                                    </div>
                                    <button
                                        type="button"
                                        className="btn-danger"
                                        style={{ minHeight: 44, padding: "0 10px", fontSize: "0.85rem" }}
                                        disabled={removeMutation.isPending}
                                        data-testid={`btn-remove-promo-${promo.id}`}
                                        onClick={async () => {
                                            const { isConfirmed } = await Swal.fire({
                                                title: "Encerrar promoção",
                                                text: `Encerrar a promoção "${promo.name}"?`,
                                                icon: "warning",
                                                showCancelButton: true,
                                                confirmButtonColor: "#d33",
                                                confirmButtonText: "Encerrar",
                                                cancelButtonText: "Voltar"
                                            });

                                            if (isConfirmed) removeMutation.mutate(promo.id);
                                        }}
                                    >
                                        Encerrar
                                    </button>
                                </div>
                            ))}
                        </div>
                    );
                })}
                {(promotionsQuery.data ?? []).length === 0 && !promotionsQuery.isLoading && (
                    <p style={{ color: "var(--ink-faint)" }} data-testid="empty-promos-msg">Nenhuma promoção programada.</p>
                )}
            </div>

            {creating && (
                <Overlay title="Nova promoção em dobro" onClose={() => setCreating(false)} data-testid="new-promo-overlay">
                    <label style={{ display: "grid", gap: 4 }}>
                        <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Nome</span>
                        <input
                            placeholder="ex.: Quarta da caipirinha"
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            data-testid="input-promo-name"
                        />
                    </label>
                    <label style={{ display: "grid", gap: 4 }}>
                        <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Produto</span>
                        <select value={productId} onChange={(e) => setProductId(e.target.value)} data-testid="select-promo-product">
                            <option value="">Selecione…</option>
                            {(menuQuery.data ?? []).map((p) => (
                                <option key={p.id} value={p.id}>{p.name}</option>
                            ))}
                        </select>
                    </label>
                    <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr 1fr" }}>
                        <label style={{ display: "grid", gap: 4 }}>
                            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Tipo</span>
                            <select value={typeId} onChange={(e) => setTypeId(Number(e.target.value))} data-testid="select-promo-type">
                                <option value={PromotionType.EmDobro}>Em dobro (paga 1 leva 2)</option>
                                <option value={PromotionType.Desconto}>Desconto %</option>
                            </select>
                        </label>
                        {typeId === PromotionType.Desconto ? (
                            <label style={{ display: "grid", gap: 4 }}>
                                <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Desconto (%)</span>
                                <input inputMode="decimal" value={discountPct} onChange={(e) => setDiscountPct(e.target.value)} data-testid="input-promo-discount" />
                            </label>
                        ) : (
                            <span />
                        )}
                    </div>
                    <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1.2fr 1fr 1fr" }}>
                        <label style={{ display: "grid", gap: 4 }}>
                            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Dia da semana</span>
                            <select value={dayOfWeek} onChange={(e) => setDayOfWeek(Number(e.target.value))} data-testid="select-promo-day">
                                {[1, 2, 3, 4, 5, 6, 0].map((d) => (
                                    <option key={d} value={d}>{dayOfWeekLabel[d]}</option>
                                ))}
                            </select>
                        </label>
                        <label style={{ display: "grid", gap: 4 }}>
                            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Início</span>
                            <input type="time" value={startTime} onChange={(e) => setStartTime(e.target.value)} data-testid="input-promo-start" />
                        </label>
                        <label style={{ display: "grid", gap: 4 }}>
                            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Fim</span>
                            <input type="time" value={endTime} onChange={(e) => setEndTime(e.target.value)} data-testid="input-promo-end" />
                        </label>
                    </div>
                    {!windowValid && (startTime !== "" || endTime !== "") && (
                        <p style={{ color: "var(--ink-faint)", fontSize: "0.85rem", margin: 0 }} data-testid="error-window-msg">
                            O horário inicial precisa ser antes do final.
                        </p>
                    )}
                    {error && <p className="error-text" data-testid="modal-error-msg">{error}</p>}
                    <button
                        type="button"
                        className="btn-primary"
                        data-testid="btn-submit-promo"
                        disabled={name.trim() === "" || productId === "" || !windowValid || !discountValid || createMutation.isPending}
                        onClick={() => createMutation.mutate()}
                    >
                        {createMutation.isPending ? "Criando…" : "Criar promoção"}
                    </button>
                </Overlay>
            )}
        </main>
    );
}