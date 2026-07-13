import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useDialog } from "../../ui/Dialog";
import {
  createEmployee,
  dismissEmployee,
  getEmployeesByBranch,
  getJobTitles,
  updateEmployee,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { formatBRL } from "../../lib/types";
import type { EmployeeResponse } from "../../lib/types";
import { Overlay } from "../orders/Overlay";
import { QueryError } from "../../components/QueryError";

const emptyForm = { jobTitleId: "", name: "", cpf: "", email: "", phone: "", salary: "" };
type FormState = typeof emptyForm;

const parseNum = (raw: string): number | null => {
  if (raw.trim() === "") return null;
  const value = Number(raw.replace(",", "."));
  return Number.isFinite(value) ? value : null;
};

export function EmployeesPage() {
  const queryClient = useQueryClient();
  const dialog = useDialog();
  const { branchId, companyId } = useAuthStore();
  const [editing, setEditing] = useState<EmployeeResponse | "new" | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [error, setError] = useState<string | null>(null);

  const employeesQuery = useQuery({
    queryKey: ["employees", branchId],
    queryFn: () => getEmployeesByBranch(branchId),
  });

  const jobTitlesQuery = useQuery({
    queryKey: ["jobtitles", companyId],
    queryFn: () => getJobTitles(companyId ?? 1),
  });

  const jobTitleName = useMemo(() => {
    const map = new Map<number, string>();
    for (const j of jobTitlesQuery.data ?? []) map.set(j.id, j.name);
    return map;
  }, [jobTitlesQuery.data]);

  const refresh = () => void queryClient.invalidateQueries({ queryKey: ["employees"] });

  const onApiError = (e: unknown) =>
    setError(e instanceof ApiError ? e.message : "Operação falhou.");

  const openEditor = (employee: EmployeeResponse | "new") => {
    setError(null);
    setEditing(employee);
    if (employee === "new")
      setForm({ ...emptyForm, jobTitleId: String(jobTitlesQuery.data?.[0]?.id ?? "") });
    else
      setForm({
        jobTitleId: String(employee.jobTitleId),
        name: employee.name,
        cpf: employee.cpf,
        email: employee.email ?? "",
        phone: employee.phone ?? "",
        salary: employee.salary === null ? "" : String(employee.salary),
      });
  };

  const saveMutation = useMutation({
    mutationFn: () => {
      const shared = {
        jobTitleId: Number(form.jobTitleId),
        name: form.name.trim(),
        email: form.email.trim() === "" ? null : form.email.trim(),
        phone: form.phone.trim() === "" ? null : form.phone.trim(),
        salary: parseNum(form.salary),
      };
      return editing === "new"
        ? createEmployee({
            branchId,
            cpf: form.cpf.trim(),
            hiredAt: new Date().toISOString(),
            ...shared,
          }).then(() => undefined)
        : updateEmployee((editing as EmployeeResponse).id, shared);
    },
    onSuccess: () => {
      setEditing(null);
      refresh();
    },
    onError: onApiError,
  });

  const dismissMutation = useMutation({
    mutationFn: (id: number) => dismissEmployee(id),
    onSuccess: refresh,
    onError: onApiError,
  });

  return (
    <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }}>
      <div className="rise" style={{ display: "flex", alignItems: "baseline", gap: 14, marginBottom: 16 }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>Equipe</h2>
        <span style={{ flex: 1 }} />
        <button className="btn-primary" onClick={() => openEditor("new")}>+ Novo funcionário</button>
      </div>

      {error && editing === null && <p className="error-text">{error}</p>}
      {employeesQuery.isError && <QueryError error={employeesQuery.error} what="funcionários" />}
      {jobTitlesQuery.isError && <QueryError error={jobTitlesQuery.error} what="cargos" />}
      {jobTitlesQuery.isSuccess && jobTitlesQuery.data.length === 0 && (
        <p className="error-text">
          Nenhum cargo cadastrado — execute BarRestaurante_JobTitles.sql para criar os cargos padrão.
        </p>
      )}

      <div className="ticket rise rise-1">
        {(employeesQuery.data ?? []).map((employee) => (
          <div className="ticket-row" key={employee.id}>
            <div style={{ display: "grid", gap: 2 }}>
              <span>{employee.name}</span>
              <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                {jobTitleName.get(employee.jobTitleId) ?? `Cargo ${employee.jobTitleId}`}
                {" · CPF "}{employee.cpf}
                {employee.salary !== null ? ` · ${formatBRL(employee.salary)}` : ""}
              </span>
            </div>
            <div style={{ display: "flex", gap: 8 }}>
              <button
                className="btn-ghost"
                style={{ minHeight: 38, padding: "0 12px", fontSize: "0.85rem" }}
                onClick={() => openEditor(employee)}
              >
                Editar
              </button>
              <button
                className="btn-danger"
                style={{ minHeight: 38, padding: "0 12px", fontSize: "0.85rem" }}
                onClick={async () => {
                  if (
                    await dialog.confirm({
                      title: "Demitir",
                      message: `Demitir "${employee.name}"? O acesso e o CPF serão liberados.`,
                      confirmLabel: "Demitir",
                      danger: true,
                    })
                  )
                    dismissMutation.mutate(employee.id);
                }}
              >
                Demitir
              </button>
            </div>
          </div>
        ))}
        {employeesQuery.data?.length === 0 && (
          <div className="ticket-row" style={{ color: "var(--ink-faint)" }}>Nenhum funcionário ativo.</div>
        )}
      </div>

      {editing !== null && (
        <Overlay title={editing === "new" ? "Novo funcionário" : "Editar funcionário"} onClose={() => setEditing(null)}>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Nome</span>
            <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          </label>
          <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr 1fr" }}>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>CPF (11 dígitos)</span>
              <input
                inputMode="numeric"
                maxLength={11}
                disabled={editing !== "new"}
                value={form.cpf}
                onChange={(e) => setForm({ ...form, cpf: e.target.value.replace(/\D/g, "") })}
              />
            </label>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Cargo</span>
              <select value={form.jobTitleId} onChange={(e) => setForm({ ...form, jobTitleId: e.target.value })}>
                <option value="">Selecione o cargo…</option>
                {(jobTitlesQuery.data ?? []).map((j) => (
                  <option key={j.id} value={j.id}>{j.name}</option>
                ))}
              </select>
            </label>
          </div>
          <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr 1fr" }}>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>E-mail</span>
              <input value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
            </label>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Telefone</span>
              <input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
            </label>
          </div>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Salário (R$, opcional)</span>
            <input inputMode="decimal" value={form.salary} onChange={(e) => setForm({ ...form, salary: e.target.value })} />
          </label>
          {error && <p className="error-text">{error}</p>}
          {form.jobTitleId === "" && (
            <p style={{ color: "var(--ink-faint)", fontSize: "0.85rem", margin: 0 }}>
              Selecione um cargo para habilitar o salvar.
            </p>
          )}
          {editing === "new" && form.cpf.length !== 11 && form.cpf.length > 0 && (
            <p style={{ color: "var(--ink-faint)", fontSize: "0.85rem", margin: 0 }}>
              CPF precisa de 11 dígitos ({form.cpf.length}/11).
            </p>
          )}
          <button
            className="btn-primary"
            disabled={
              form.name.trim() === "" ||
              form.jobTitleId === "" ||
              (editing === "new" && form.cpf.length !== 11) ||
              saveMutation.isPending
            }
            onClick={() => saveMutation.mutate()}
          >
            {saveMutation.isPending ? "Salvando…" : "Salvar"}
          </button>
        </Overlay>
      )}
    </main>
  );
}
