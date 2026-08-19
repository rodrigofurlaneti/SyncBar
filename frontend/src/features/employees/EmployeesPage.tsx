import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useDialog } from "../../ui/Dialog";
import {
  createEmployee,
  createJobTitle,
  dismissEmployee,
  getEmployeesByBranch,
  getJobTitles,
  updateEmployee,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { formatBRL } from "../../lib/types";
import type { EmployeeResponse } from "../../lib/types";
import { QueryError } from "../../components/QueryError";
import { EmptyState } from "../../ui/EmptyState";
import { SkeletonList } from "../../ui/Skeleton";
import { Modal } from "../../ui/Modal";
import { Button } from "../../ui/Button";
import { Field, TextField } from "../../ui/Field";
import { useToast } from "../../ui/Toast";

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
  const toast = useToast();
  const { branchId, companyId } = useAuthStore();
  const [editing, setEditing] = useState<EmployeeResponse | "new" | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [error, setError] = useState<string | null>(null);
  const [creatingJobTitle, setCreatingJobTitle] = useState(false);
  const [newJobTitle, setNewJobTitle] = useState("");

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
    setCreatingJobTitle(false);
    setNewJobTitle("");
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
      toast.success(editing === "new" ? "Funcionário cadastrado." : "Funcionário atualizado.");
      setEditing(null);
      refresh();
    },
    onError: onApiError,
  });

  const dismissMutation = useMutation({
    mutationFn: (id: number) => dismissEmployee(id),
    onSuccess: () => {
      toast.success("Funcionário demitido.");
      refresh();
    },
    onError: onApiError,
  });

  // Criar cargo sem sair do formulário de novo funcionário — o cargo novo já
  // entra selecionado assim que criado (mesmo padrão do "+ nova categoria" no
  // cadastro de produto).
  const jobTitleMutation = useMutation({
    mutationFn: () => createJobTitle(companyId ?? 1, newJobTitle.trim()),
    onSuccess: (newJobTitleId) => {
      toast.success("Cargo criado.");
      setForm((f) => ({ ...f, jobTitleId: String(newJobTitleId) }));
      setNewJobTitle("");
      setCreatingJobTitle(false);
      setError(null);
      void queryClient.invalidateQueries({ queryKey: ["jobtitles"] });
    },
    onError: onApiError,
  });

  return (
    <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }}>
      <div className="rise" style={{ display: "flex", alignItems: "baseline", gap: 14, marginBottom: 16 }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>Equipe</h2>
        <span style={{ flex: 1 }} />
        <Button variant="primary" onClick={() => openEditor("new")}>+ Novo funcionário</Button>
      </div>

      {error && editing === null && <p className="error-text">{error}</p>}
      {employeesQuery.isError && <QueryError error={employeesQuery.error} what="funcionários" />}
      {jobTitlesQuery.isError && <QueryError error={jobTitlesQuery.error} what="cargos" />}

      {employeesQuery.isLoading && <SkeletonList rows={5} rowHeight={58} />}

      {!employeesQuery.isLoading && employeesQuery.data?.length === 0 && (
        <EmptyState
          icon="🧑‍🍳"
          title="Nenhum funcionário ativo"
          description="Cadastre a equipe para poder abrir usuários e vincular vendas a um responsável."
          action={
            <Button variant="primary" onClick={() => openEditor("new")}>
              + Novo funcionário
            </Button>
          }
        />
      )}

      {!employeesQuery.isLoading && (employeesQuery.data?.length ?? 0) > 0 && (
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
              <div className="ui-row" style={{ gap: 8 }}>
                <Button variant="ghost" size="sm" onClick={() => openEditor(employee)}>
                  Editar
                </Button>
                <Button
                  variant="danger"
                  size="sm"
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
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      {editing !== null && (
        <Modal title={editing === "new" ? "Novo funcionário" : "Editar funcionário"} onClose={() => setEditing(null)}>
          <TextField
            label="Nome"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            autoFocus
          />

          {/* alignItems: "end" — o label "Cargo" pode quebrar em 2 linhas por causa do
              link "+ novo cargo" embutido nele; alinhando pelo rodapé da linha, os dois
              campos ficam sempre na mesma altura (mesmo ajuste feito no cadastro de produto). */}
          <div className="ui-row ui-row-wrap" style={{ alignItems: "end" }}>
            <div style={{ flex: 1, minWidth: 160 }}>
              <TextField
                label="CPF (11 dígitos)"
                inputMode="numeric"
                maxLength={11}
                disabled={editing !== "new"}
                value={form.cpf}
                onChange={(e) => setForm({ ...form, cpf: e.target.value.replace(/\D/g, "") })}
              />
            </div>
            <div style={{ flex: 1, minWidth: 220 }}>
              <Field
                label={
                  <span className="ui-row" style={{ justifyContent: "space-between", width: "100%" }}>
                    Cargo
                    {!creatingJobTitle && (
                      <button
                        type="button"
                        onClick={() => setCreatingJobTitle(true)}
                        style={{ background: "transparent", border: "none", color: "var(--amber)", cursor: "pointer", fontSize: "0.8rem", padding: 0 }}
                      >
                        + novo cargo
                      </button>
                    )}
                  </span>
                }
              >
                {(a11y) =>
                  creatingJobTitle ? (
                    <div className="ui-row" style={{ gap: 6 }}>
                      <input
                        {...a11y}
                        type="text"
                        autoFocus
                        placeholder="Nome do cargo"
                        value={newJobTitle}
                        onChange={(e) => setNewJobTitle(e.target.value)}
                        onKeyDown={(e) => {
                          if (e.key === "Enter") {
                            e.preventDefault();
                            if (newJobTitle.trim() !== "") jobTitleMutation.mutate();
                          } else if (e.key === "Escape") {
                            setCreatingJobTitle(false);
                            setNewJobTitle("");
                          }
                        }}
                        style={{ flex: 1, minWidth: 0 }}
                      />
                      <Button
                        size="sm"
                        loading={jobTitleMutation.isPending}
                        disabled={newJobTitle.trim() === ""}
                        onClick={() => jobTitleMutation.mutate()}
                      >
                        Criar
                      </Button>
                      <Button
                        size="sm"
                        iconOnly
                        aria-label="Cancelar criação de cargo"
                        onClick={() => { setCreatingJobTitle(false); setNewJobTitle(""); }}
                      >
                        ✕
                      </Button>
                    </div>
                  ) : (
                    <select
                      {...a11y}
                      value={form.jobTitleId}
                      onChange={(e) => setForm({ ...form, jobTitleId: e.target.value })}
                    >
                      <option value="">Selecione o cargo…</option>
                      {(jobTitlesQuery.data ?? []).map((j) => (
                        <option key={j.id} value={j.id}>{j.name}</option>
                      ))}
                    </select>
                  )
                }
              </Field>
            </div>
          </div>

          <div className="ui-row ui-row-wrap">
            <div style={{ flex: 1, minWidth: 160 }}>
              <TextField
                label="E-mail"
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
              />
            </div>
            <div style={{ flex: 1, minWidth: 160 }}>
              <TextField
                label="Telefone"
                value={form.phone}
                onChange={(e) => setForm({ ...form, phone: e.target.value })}
              />
            </div>
          </div>

          <TextField
            label="Salário (R$, opcional)"
            inputMode="decimal"
            value={form.salary}
            onChange={(e) => setForm({ ...form, salary: e.target.value })}
          />

          {error && <p className="error-text">{error}</p>}
          {form.jobTitleId === "" && (
            <p className="field-hint" style={{ margin: 0 }}>
              Selecione um cargo para habilitar o salvar.
            </p>
          )}
          {editing === "new" && form.cpf.length !== 11 && form.cpf.length > 0 && (
            <p className="field-hint" style={{ margin: 0 }}>
              CPF precisa de 11 dígitos ({form.cpf.length}/11).
            </p>
          )}

          <Button
            variant="primary"
            block
            loading={saveMutation.isPending}
            disabled={
              form.name.trim() === "" ||
              form.jobTitleId === "" ||
              (editing === "new" && form.cpf.length !== 11)
            }
            onClick={() => saveMutation.mutate()}
          >
            Salvar
          </Button>
        </Modal>
      )}
    </main>
  );
}
