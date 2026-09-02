import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useDialog } from "../../ui/Dialog";
import {
  createEmployee,
  createJobTitle,
  dismissEmployee,
  getEmployeesByBranch,
  getJobTitles,
  registerTeamMember,
  updateEmployee,
} from "./api";
import type { RegisterTeamMemberResult } from "./api";
import { getFeatures, getJobTitleFeatures, getUserFeatures, setUserFeatures } from "../access/api";
import { deactivateUser } from "../users/api";
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
import { Switch } from "../../ui/Switch";
import { useToast } from "../../ui/Toast";

const emptyForm = { jobTitleId: "", name: "", cpf: "", email: "", phone: "", salary: "" };
type FormState = typeof emptyForm;

const emptyAccessForm = { hasSystemAccess: false, userName: "", userEmail: "", password: "" };
type AccessFormState = typeof emptyAccessForm;

type StatusFilter = "all" | "access" | "no-login";

const parseNum = (raw: string): number | null => {
  if (raw.trim() === "") return null;
  const value = Number(raw.replace(",", "."));
  return Number.isFinite(value) ? value : null;
};

const initialsOf = (name: string) =>
  name
    .trim()
    .split(/\s+/)
    .map((p) => p[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();

/* Ícones inline (sem dependência externa), no mesmo estilo do resto da UI (Cardápio). */
const SearchIcon = () => (
  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" aria-hidden="true">
    <circle cx="11" cy="11" r="7" stroke="currentColor" strokeWidth="1.8" />
    <path d="M21 21l-4.3-4.3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
  </svg>
);
const KebabIcon = () => (
  <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
    <circle cx="12" cy="5" r="1.8" /><circle cx="12" cy="12" r="1.8" /><circle cx="12" cy="19" r="1.8" />
  </svg>
);
const CheckIcon = () => (
  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" aria-hidden="true">
    <path d="M4 12l5 5L20 6" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round" />
  </svg>
);
const PerfilIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
    <circle cx="12" cy="8" r="4" stroke="currentColor" strokeWidth="2" />
    <path d="M4 20c0-4 3.6-7 8-7s8 3 8 7" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
  </svg>
);

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

  // Acesso ao sistema — só existe no fluxo de "novo funcionário". Cargo (cima) e acessos (aqui
  // embaixo) ficam na MESMA tela: nem todo funcionário da equipe usa o sistema (auxiliar de
  // limpeza, vigilante), então isto começa desligado e só pede usuário/senha quando ligado.
  const [access, setAccess] = useState<AccessFormState>(emptyAccessForm);
  const [extraFeatureIds, setExtraFeatureIds] = useState<number[]>([]);

  // Busca/filtro da grade de cartões + menu de ações (kebab) + painel de acessos por pessoa —
  // isto é o que substitui ter que visitar "Usuários e perfis" e "Acessos" separadamente.
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [openKebabFor, setOpenKebabFor] = useState<number | null>(null);
  const [accessDrawerFor, setAccessDrawerFor] = useState<EmployeeResponse | null>(null);
  const [drawerExtras, setDrawerExtras] = useState<number[]>([]);
  const [drawerLoadedFor, setDrawerLoadedFor] = useState<number | null>(null);

  const employeesQuery = useQuery({
    queryKey: ["employees", branchId],
    queryFn: () => getEmployeesByBranch(branchId),
  });

  const jobTitlesQuery = useQuery({
    queryKey: ["jobtitles", companyId],
    queryFn: () => getJobTitles(companyId ?? 1),
  });

  const featuresQuery = useQuery({
    queryKey: ["access", "features"],
    queryFn: getFeatures,
    enabled: (editing === "new" && access.hasSystemAccess) || accessDrawerFor !== null,
  });

  const jobTitleIdNum = form.jobTitleId === "" ? null : Number(form.jobTitleId);

  // Telas que o Cargo já libera por padrão (JobTitleFeature) — mostradas como referência
  // ("já incluso via cargo") para a pessoa não marcar de novo o que já vem de graça.
  const cargoFeaturesQuery = useQuery({
    queryKey: ["access", "jobtitlefeatures", jobTitleIdNum],
    queryFn: () => getJobTitleFeatures(jobTitleIdNum as number),
    enabled: editing === "new" && access.hasSystemAccess && jobTitleIdNum !== null,
  });
  const cargoFeatureIds = useMemo(() => new Set(cargoFeaturesQuery.data ?? []), [cargoFeaturesQuery.data]);

  // Mesma lógica do modal, agora para o painel de acessos de uma pessoa já cadastrada.
  const drawerCargoFeaturesQuery = useQuery({
    queryKey: ["access", "jobtitlefeatures", accessDrawerFor?.jobTitleId ?? null],
    queryFn: () => getJobTitleFeatures(accessDrawerFor!.jobTitleId),
    enabled: accessDrawerFor !== null,
  });
  const drawerCargoFeatureIds = useMemo(
    () => new Set(drawerCargoFeaturesQuery.data ?? []),
    [drawerCargoFeaturesQuery.data],
  );
  const drawerUserFeaturesQuery = useQuery({
    queryKey: ["access", "userfeatures", accessDrawerFor?.appUserId ?? null],
    queryFn: () => getUserFeatures(accessDrawerFor!.appUserId as number),
    enabled: accessDrawerFor !== null && accessDrawerFor.appUserId !== null,
  });
  useEffect(() => {
    if (
      accessDrawerFor !== null &&
      drawerUserFeaturesQuery.data !== undefined &&
      drawerLoadedFor !== accessDrawerFor.id
    ) {
      setDrawerExtras(drawerUserFeaturesQuery.data);
      setDrawerLoadedFor(accessDrawerFor.id);
    }
  }, [accessDrawerFor, drawerUserFeaturesQuery.data, drawerLoadedFor]);

  // Ao trocar o cargo, os "extras" marcados eram relativos ao cargo anterior — evita salvar
  // acesso extra para um cargo que a pessoa nem escolheu mais.
  useEffect(() => {
    setExtraFeatureIds([]);
  }, [jobTitleIdNum]);

  const jobTitleName = useMemo(() => {
    const map = new Map<number, string>();
    for (const j of jobTitlesQuery.data ?? []) map.set(j.id, j.name);
    return map;
  }, [jobTitlesQuery.data]);

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ["employees"] });
    void queryClient.invalidateQueries({ queryKey: ["users"] });
    void queryClient.invalidateQueries({ queryKey: ["roles"] });
    void queryClient.invalidateQueries({ queryKey: ["access"] });
  };

  const onApiError = (e: unknown) =>
    setError(e instanceof ApiError ? e.message : "Operação falhou.");

  const openEditor = (employee: EmployeeResponse | "new") => {
    setError(null);
    setCreatingJobTitle(false);
    setNewJobTitle("");
    setAccess(emptyAccessForm);
    setExtraFeatureIds([]);
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

  // Liga o toggle "usa o sistema" pré-preenchendo o e-mail de login com o e-mail do funcionário
  // (evita digitar o mesmo e-mail duas vezes) — a pessoa ainda pode editar antes de salvar.
  const toggleSystemAccess = (checked: boolean) =>
    setAccess((a) => ({
      ...a,
      hasSystemAccess: checked,
      userEmail: checked && a.userEmail === "" ? form.email.trim() : a.userEmail,
    }));

  const saveMutation = useMutation({
    // Retorno unificado como Promise<RegisterTeamMemberResult | undefined> — updateEmployee e
    // createEmployee resolvem em void, mas o TS não infere um tipo único de TData a partir de
    // um corpo com "return" de Promises diferentes; a anotação explícita resolve isso e mantém
    // onSuccess tipado (em vez de cair em "never").
    mutationFn: async (): Promise<RegisterTeamMemberResult | undefined> => {
      const shared = {
        jobTitleId: Number(form.jobTitleId),
        name: form.name.trim(),
        email: form.email.trim() === "" ? null : form.email.trim(),
        phone: form.phone.trim() === "" ? null : form.phone.trim(),
        salary: parseNum(form.salary),
      };

      if (editing !== "new") {
        await updateEmployee((editing as EmployeeResponse).id, shared);
        return undefined;
      }

      if (!access.hasSystemAccess) {
        await createEmployee({
          branchId,
          cpf: form.cpf.trim(),
          hiredAt: new Date().toISOString(),
          ...shared,
        });
        return undefined;
      }

      return registerTeamMember(companyId ?? 1, {
        branchId,
        cpf: form.cpf.trim(),
        hiredAt: new Date().toISOString(),
        ...shared,
        hasSystemAccess: true,
        userName: access.userName.trim(),
        userEmail: access.userEmail.trim(),
        password: access.password,
        extraFeatureIds: extraFeatureIds.length > 0 ? extraFeatureIds : null,
      });
    },
    onSuccess: (result) => {
      toast.success(editing === "new" ? "Funcionário cadastrado." : "Funcionário atualizado.");
      // registerTeamMember pode "degradar graciosamente": funcionário criado, mas o usuário não
      // (ex.: username já em uso) — avisamos sem esconder que o cadastro do funcionário valeu.
      if (result && typeof result === "object" && "accessWarning" in result && result.accessWarning) {
        toast.error(result.accessWarning);
      }
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

  const toggleExtraFeature = (featureId: number) =>
    setExtraFeatureIds((current) =>
      current.includes(featureId) ? current.filter((id) => id !== featureId) : [...current, featureId],
    );

  const accessFieldsValid =
    !access.hasSystemAccess ||
    (access.userName.trim() !== "" && access.userEmail.trim() !== "" && access.password.length >= 8);

  // --- Painel de acessos por pessoa (drawer) — substitui ter que ir em Usuários/Acessos ---
  const closeDrawer = () => {
    setAccessDrawerFor(null);
    setDrawerExtras([]);
    setDrawerLoadedFor(null);
    setError(null);
  };
  const toggleDrawerExtra = (featureId: number) =>
    setDrawerExtras((current) =>
      current.includes(featureId) ? current.filter((id) => id !== featureId) : [...current, featureId],
    );
  const drawerSaveMutation = useMutation({
    mutationFn: () => setUserFeatures(accessDrawerFor!.appUserId as number, drawerExtras),
    onSuccess: () => {
      toast.success("Acessos atualizados.");
      closeDrawer();
      refresh();
    },
    onError: onApiError,
  });
  const deactivateLoginMutation = useMutation({
    mutationFn: () => deactivateUser(accessDrawerFor!.appUserId as number),
    onSuccess: () => {
      toast.success("Login desativado — o funcionário continua na equipe.");
      closeDrawer();
      refresh();
    },
    onError: onApiError,
  });

  const employees = employeesQuery.data ?? [];
  const withAccessCount = employees.filter((e) => e.hasSystemAccess).length;
  const noLoginCount = employees.length - withAccessCount;

  const filteredEmployees = useMemo(() => {
    let list = employees;
    if (statusFilter === "access") list = list.filter((e) => e.hasSystemAccess);
    if (statusFilter === "no-login") list = list.filter((e) => !e.hasSystemAccess);
    if (search.trim() !== "") {
      const q = search.trim().toLowerCase();
      list = list.filter(
        (e) => e.name.toLowerCase().includes(q) || (jobTitleName.get(e.jobTitleId) ?? "").toLowerCase().includes(q),
      );
    }
    return list;
  }, [employees, statusFilter, search, jobTitleName]);

  return (
    <main style={{ padding: 22, maxWidth: 1280, margin: "0 auto" }}>
      <div className="emp-tabs rise">
        <span className="emp-tab is-active">Pessoas</span>
        <Link to="/acessos" className="emp-tab">Cargos e acessos padrão</Link>
      </div>

      <div className="rise" style={{ display: "flex", alignItems: "flex-end", justifyContent: "space-between", gap: 20, flexWrap: "wrap" }}>
        <div>
          <h2 className="display" style={{ fontSize: "1.7rem" }}>Equipe</h2>
          <p style={{ color: "var(--ink-dim)", fontSize: "0.9rem", marginTop: 6, maxWidth: 480 }}>
            Todo mundo que trabalha na casa fica aqui — nem todos precisam de acesso ao sistema.
          </p>
        </div>
        <Button variant="primary" onClick={() => openEditor("new")}>+ Novo funcionário</Button>
      </div>

      {error && editing === null && accessDrawerFor === null && <p className="error-text">{error}</p>}
      {employeesQuery.isError && <QueryError error={employeesQuery.error} what="funcionários" />}
      {jobTitlesQuery.isError && <QueryError error={jobTitlesQuery.error} what="cargos" />}

      {!employeesQuery.isLoading && employees.length > 0 && (
        <div className="emp-toolbar rise rise-1">
          <div className="emp-search">
            <SearchIcon />
            <input
              placeholder="Buscar por nome ou cargo…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <div className="segmented" role="group" aria-label="Filtrar por acesso">
            <button type="button" className={statusFilter === "all" ? "is-active" : ""} onClick={() => setStatusFilter("all")}>
              Todos · {employees.length}
            </button>
            <button type="button" className={statusFilter === "access" ? "is-active" : ""} onClick={() => setStatusFilter("access")}>
              Com acesso · {withAccessCount}
            </button>
            <button type="button" className={statusFilter === "no-login" ? "is-active" : ""} onClick={() => setStatusFilter("no-login")}>
              Sem login · {noLoginCount}
            </button>
          </div>
        </div>
      )}

      {employeesQuery.isLoading && <SkeletonList rows={5} rowHeight={58} />}

      {!employeesQuery.isLoading && employees.length === 0 && (
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

      {!employeesQuery.isLoading && employees.length > 0 && filteredEmployees.length === 0 && (
        <EmptyState
          icon="🔍"
          title="Nenhum funcionário encontrado"
          description={`Nenhum resultado para "${search.trim()}" com o filtro atual.`}
        />
      )}

      {!employeesQuery.isLoading && filteredEmployees.length > 0 && (
        <div className="emp-grid rise rise-1">
          {filteredEmployees.map((employee) => (
            <div className={`emp-card ${!employee.hasSystemAccess ? "is-no-login" : ""}`} key={employee.id}>
              <div className="emp-card-top">
                <div className="emp-avatar">{initialsOf(employee.name)}</div>
                <div className="emp-name-block">
                  <div className="emp-name">{employee.name}</div>
                  <div className="emp-role">{jobTitleName.get(employee.jobTitleId) ?? `Cargo ${employee.jobTitleId}`}</div>
                </div>
                <span className={`emp-status-pill ${employee.hasSystemAccess ? "is-on" : "is-off"}`}>
                  <span className="emp-status-dot" />
                  {employee.hasSystemAccess ? "Acesso ativo" : "Sem login"}
                </span>
              </div>

              <div className="emp-meta">
                CPF {employee.cpf}
                {employee.salary !== null ? ` · ${formatBRL(employee.salary)}` : ""}
              </div>

              {employee.hasSystemAccess && (
                <div className="emp-chip-row">
                  <span className="emp-perfil-chip">
                    <PerfilIcon />
                    {employee.roleName ?? "Perfil"}
                  </span>
                  {employee.extraFeatureCount > 0 && (
                    <span className="emp-extra-chip">
                      +{employee.extraFeatureCount} acesso{employee.extraFeatureCount > 1 ? "s" : ""} extra
                      {employee.extraFeatureCount > 1 ? "s" : ""}
                    </span>
                  )}
                </div>
              )}

              <div className="emp-card-footer">
                <Button size="sm" onClick={() => openEditor(employee)}>Editar</Button>
                {employee.hasSystemAccess && (
                  <Button
                    size="sm"
                    onClick={() => {
                      setError(null);
                      setAccessDrawerFor(employee);
                    }}
                  >
                    Acessos
                  </Button>
                )}
                <span className="ui-spacer" />
                <Button
                  size="sm"
                  iconOnly
                  aria-label="Mais ações"
                  onClick={() => setOpenKebabFor(openKebabFor === employee.id ? null : employee.id)}
                >
                  <KebabIcon />
                </Button>
                {openKebabFor === employee.id && (
                  <>
                    <div
                      style={{ position: "fixed", inset: 0, zIndex: 4 }}
                      onClick={() => setOpenKebabFor(null)}
                    />
                    <div className="emp-kebab-menu">
                      <button
                        type="button"
                        className="is-danger"
                        onClick={async () => {
                          setOpenKebabFor(null);
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
                  </>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {editing !== null && (
        <Modal title={editing === "new" ? "Novo funcionário" : "Editar funcionário"} onClose={() => setEditing(null)} wide>
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

          {editing === "new" && (
            <div style={{ display: "grid", gap: 14, marginTop: 4 }}>
              <div className="emp-divider-label">
                <span className="emp-divider-line" />
                <span>Acesso ao sistema</span>
                <span className="emp-divider-line" />
              </div>

              <div className="emp-toggle-row">
                <div className="emp-toggle-copy">
                  <div className="emp-toggle-title">Este colaborador usa o sistema</div>
                  <div className="emp-toggle-hint">
                    Ative só para quem vai operar o PDV — copa, limpeza e segurança normalmente ficam desligados.
                  </div>
                </div>
                <Switch
                  checked={access.hasSystemAccess}
                  onChange={toggleSystemAccess}
                  label="Este colaborador usa o sistema"
                />
              </div>

              {access.hasSystemAccess && (
                <div style={{ display: "grid", gap: 14 }}>
                  <div className="ui-row ui-row-wrap">
                    <div style={{ flex: 1, minWidth: 160 }}>
                      <TextField
                        label="Usuário"
                        value={access.userName}
                        onChange={(e) => setAccess({ ...access, userName: e.target.value })}
                      />
                    </div>
                    <div style={{ flex: 1, minWidth: 160 }}>
                      <TextField
                        label="E-mail de login"
                        value={access.userEmail}
                        onChange={(e) => setAccess({ ...access, userEmail: e.target.value })}
                      />
                    </div>
                  </div>
                  <TextField
                    label="Senha (mín. 8 caracteres)"
                    type="password"
                    value={access.password}
                    onChange={(e) => setAccess({ ...access, password: e.target.value })}
                  />

                  {jobTitleIdNum !== null && (
                    <div className="emp-perfil-auto-card">
                      <div className="emp-perfil-auto-icon"><PerfilIcon /></div>
                      <div className="emp-perfil-auto-copy">
                        <b>Perfil: {jobTitleName.get(jobTitleIdNum) ?? "—"}</b>
                        <span>Criado automaticamente a partir do cargo — sem passo extra.</span>
                      </div>
                    </div>
                  )}

                  {featuresQuery.isError && <QueryError error={featuresQuery.error} what="as telas" />}

                  {cargoFeatureIds.size > 0 && (
                    <div>
                      <div className="emp-access-group-label">O cargo já libera:</div>
                      <div className="emp-chips-wrap">
                        {(featuresQuery.data ?? [])
                          .filter((f) => cargoFeatureIds.has(f.id))
                          .map((f) => (
                            <span key={f.id} className="emp-access-chip is-locked">
                              <CheckIcon /> {f.name}
                            </span>
                          ))}
                      </div>
                    </div>
                  )}

                  <div>
                    <div className="emp-access-group-label">Acessos extras só para esta pessoa</div>
                    <div className="emp-chips-wrap">
                      {(featuresQuery.data ?? [])
                        .filter((f) => !cargoFeatureIds.has(f.id))
                        .map((f) => {
                          const on = extraFeatureIds.includes(f.id);
                          return (
                            <button
                              key={f.id}
                              type="button"
                              className={`emp-access-chip ${on ? "is-on" : "is-off"}`}
                              onClick={() => toggleExtraFeature(f.id)}
                            >
                              {on && <CheckIcon />} {f.name}
                            </button>
                          );
                        })}
                    </div>
                  </div>
                </div>
              )}
            </div>
          )}

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
              (editing === "new" && form.cpf.length !== 11) ||
              !accessFieldsValid
            }
            onClick={() => saveMutation.mutate()}
          >
            Salvar
          </Button>
        </Modal>
      )}

      {accessDrawerFor !== null && (
        <Modal
          title={`Acessos — ${accessDrawerFor.name}`}
          onClose={closeDrawer}
          variant="drawer"
          ariaLabel={`Acessos de ${accessDrawerFor.name}`}
        >
          <p className="emp-info-line">
            O acesso efetivo é a soma do que o cargo já dá com o que for ligado abaixo, só para esta pessoa.
          </p>

          {drawerCargoFeaturesQuery.isError && <QueryError error={drawerCargoFeaturesQuery.error} what="os acessos do cargo" />}
          {drawerUserFeaturesQuery.isError && <QueryError error={drawerUserFeaturesQuery.error} what="os acessos da pessoa" />}
          {featuresQuery.isError && <QueryError error={featuresQuery.error} what="as telas" />}

          {drawerCargoFeatureIds.size > 0 && (
            <div>
              <div className="emp-access-group-label" style={{ marginBottom: 8 }}>
                Já incluso pelo cargo "{jobTitleName.get(accessDrawerFor.jobTitleId) ?? ""}"
              </div>
              <div className="emp-locked-list">
                {(featuresQuery.data ?? [])
                  .filter((f) => drawerCargoFeatureIds.has(f.id))
                  .map((f) => (
                    <div className="emp-locked-row" key={f.id}>
                      <CheckIcon /> {f.name}
                    </div>
                  ))}
              </div>
            </div>
          )}

          <div>
            <div className="emp-access-group-label">Acessos extras desta pessoa</div>
            <div className="emp-toggle-list">
              {(featuresQuery.data ?? [])
                .filter((f) => !drawerCargoFeatureIds.has(f.id))
                .map((f) => (
                  <div className="emp-toggle-item" key={f.id}>
                    <span>{f.name}</span>
                    <Switch
                      checked={drawerExtras.includes(f.id)}
                      onChange={() => toggleDrawerExtra(f.id)}
                      label={drawerExtras.includes(f.id) ? `Remover acesso a ${f.name}` : `Conceder acesso a ${f.name}`}
                    />
                  </div>
                ))}
            </div>
          </div>

          {error && <p className="error-text">{error}</p>}

          <Button
            variant="primary"
            block
            loading={drawerSaveMutation.isPending}
            disabled={drawerUserFeaturesQuery.isLoading}
            onClick={() => drawerSaveMutation.mutate()}
          >
            Salvar acessos
          </Button>

          <div className="emp-danger-row" style={{ borderTop: "1px solid var(--line)", paddingTop: 14, marginTop: 2 }}>
            <div className="emp-danger-copy">
              <b>Desativar login</b>
              <span>O funcionário continua na equipe, só perde o acesso ao sistema.</span>
            </div>
            <Button
              variant="danger"
              size="sm"
              loading={deactivateLoginMutation.isPending}
              onClick={async () => {
                if (
                  await dialog.confirm({
                    title: "Desativar login",
                    message: `Desativar o login de "${accessDrawerFor.name}"? O funcionário continua na equipe.`,
                    confirmLabel: "Desativar",
                    danger: true,
                  })
                )
                  deactivateLoginMutation.mutate();
              }}
            >
              Desativar
            </Button>
          </div>
        </Modal>
      )}
    </main>
  );
}
