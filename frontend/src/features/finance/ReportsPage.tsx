import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { getSalesReport } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { dayOfWeekLabel, formatBRL } from "../../lib/types";
import { QueryError } from "../../components/QueryError";

function currentMonthValue(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}`;
}

function Bar({ value, max, label, sub }: { value: number; max: number; label: string; sub: string }) {
  return (
    <div style={{ display: "grid", gap: 3 }}>
      <div style={{ display: "flex", justifyContent: "space-between", fontSize: "0.85rem" }}>
        <span>{label}</span>
        <span className="mono-num" style={{ color: "var(--ink-dim)" }}>{sub}</span>
      </div>
      <div style={{ background: "var(--bg-press)", borderRadius: 999, height: 10, overflow: "hidden" }}>
        <div
          style={{
            width: `${max <= 0 ? 0 : Math.max(2, (value / max) * 100)}%`,
            height: "100%",
            background: "var(--amber)",
            borderRadius: 999,
          }}
        />
      </div>
    </div>
  );
}

export function ReportsPage() {
  const { branchId } = useAuthStore();
  const [monthValue, setMonthValue] = useState(currentMonthValue());
  const [yearStr, monthStr] = monthValue.split("-");
  const year = Number(yearStr);
  const month = Number(monthStr);

  const reportQuery = useQuery({
    queryKey: ["report", branchId, year, month],
    queryFn: () => getSalesReport(branchId, year, month),
    enabled: Number.isFinite(year) && Number.isFinite(month),
  });

  const report = reportQuery.data;
  const maxProduct = Math.max(1, ...(report?.topProducts ?? []).map((p) => p.revenue));
  const maxWeekday = Math.max(1, ...(report?.revenueByWeekday ?? []).map((d) => d.revenue));
  const maxHour = Math.max(1, ...(report?.revenueByHour ?? []).map((h) => h.revenue));

  return (
    <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }}>
      <div className="rise" style={{ display: "flex", alignItems: "center", gap: 14, marginBottom: 16, flexWrap: "wrap" }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>Relatórios</h2>
        <span style={{ flex: 1 }} />
        <input type="month" value={monthValue} onChange={(e) => setMonthValue(e.target.value)} style={{ width: 190 }} />
      </div>

      {reportQuery.isError && <QueryError error={reportQuery.error} what="o relatório" />}

      {report && (
        <>
          <div className="rise rise-1" style={{ display: "grid", gap: 12, gridTemplateColumns: "repeat(auto-fit, minmax(170px, 1fr))", marginBottom: 16 }}>
            {[
              { label: "Receita", value: formatBRL(report.revenue), tone: "var(--amber)" },
              { label: `Vendas`, value: String(report.salesCount), tone: "var(--ink)" },
              { label: "Ticket médio", value: formatBRL(report.averageTicket), tone: "var(--ink)" },
              { label: "Taxa de serviço", value: formatBRL(report.serviceFeeTotal), tone: "var(--ok)" },
              { label: "Itens cancelados", value: String(report.cancelledItemsCount), tone: report.cancelledItemsCount > 0 ? "var(--danger)" : "var(--ok)" },
            ].map((card) => (
              <div className="ticket" key={card.label} style={{ padding: "14px 18px", display: "grid", gap: 4 }}>
                <span style={{ fontFamily: "var(--font-cond)", textTransform: "uppercase", letterSpacing: "0.08em", fontSize: "0.78rem", color: "var(--ink-faint)" }}>
                  {card.label}
                </span>
                <span className="mono-num display" style={{ fontSize: "1.6rem", color: card.tone }}>{card.value}</span>
              </div>
            ))}
          </div>

          <div className="rise rise-2" style={{ display: "grid", gap: 14, gridTemplateColumns: "repeat(auto-fit, minmax(320px, 1fr))" }}>
            <div className="ticket" style={{ padding: 18, display: "grid", gap: 10, alignContent: "start" }}>
              <span className="display" style={{ fontSize: "1.2rem" }}>Top produtos</span>
              {report.topProducts.map((p) => (
                <Bar key={p.productId} value={p.revenue} max={maxProduct}
                  label={p.productName} sub={`${p.quantity} un · ${formatBRL(p.revenue)}`} />
              ))}
              {report.topProducts.length === 0 && <span style={{ color: "var(--ink-faint)" }}>Sem vendas no mês.</span>}
            </div>

            <div className="ticket" style={{ padding: 18, display: "grid", gap: 10, alignContent: "start" }}>
              <span className="display" style={{ fontSize: "1.2rem" }}>Vendas por funcionário</span>
              {report.salesByEmployee.map((e) => (
                <Bar key={e.employeeId} value={e.revenue} max={Math.max(1, report.salesByEmployee[0]?.revenue ?? 1)}
                  label={e.employeeName}
                  sub={`${e.salesCount} vendas · ${formatBRL(e.revenue)} · serviço ${formatBRL(e.serviceFee)}`} />
              ))}
              {report.salesByEmployee.length === 0 && <span style={{ color: "var(--ink-faint)" }}>Sem vendas no mês.</span>}
            </div>

            <div className="ticket" style={{ padding: 18, display: "grid", gap: 10, alignContent: "start" }}>
              <span className="display" style={{ fontSize: "1.2rem" }}>Receita por dia da semana</span>
              {[1, 2, 3, 4, 5, 6, 0].map((d) => {
                const row = report.revenueByWeekday.find((w) => w.dayOfWeek === d);
                return (
                  <Bar key={d} value={row?.revenue ?? 0} max={maxWeekday}
                    label={dayOfWeekLabel[d]} sub={`${row?.salesCount ?? 0} vendas · ${formatBRL(row?.revenue ?? 0)}`} />
                );
              })}
            </div>

            <div className="ticket" style={{ padding: 18, display: "grid", gap: 10, alignContent: "start" }}>
              <span className="display" style={{ fontSize: "1.2rem" }}>Horários de pico</span>
              {report.revenueByHour.map((h) => (
                <Bar key={h.hour} value={h.revenue} max={maxHour}
                  label={`${String(h.hour).padStart(2, "0")}h`} sub={formatBRL(h.revenue)} />
              ))}
              {report.revenueByHour.length === 0 && <span style={{ color: "var(--ink-faint)" }}>Sem vendas no mês.</span>}
            </div>
          </div>

          {report.cancelledItems.length > 0 && (
            <div className="ticket rise rise-3" style={{ marginTop: 14 }}>
              <div className="ticket-head">
                <span className="display" style={{ fontSize: "1.2rem", color: "var(--danger)" }}>
                  Cancelamentos ({report.cancelledItemsCount})
                </span>
              </div>
              {report.cancelledItems.map((item, index) => (
                <div className="ticket-row" key={index}>
                  <span>
                    {item.quantity} × {item.productName}
                    <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)", marginLeft: 8 }}>
                      {new Date(item.cancelledAt).toLocaleString("pt-BR", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" })}
                    </span>
                  </span>
                  <span style={{ color: item.cancelledBy ? "var(--ink-dim)" : "var(--danger)", fontSize: "0.85rem" }}>
                    {item.cancelledBy ? `por ${item.cancelledBy}` : "sem responsável"}
                  </span>
                </div>
              ))}
            </div>
          )}
        </>
      )}
    </main>
  );
}
