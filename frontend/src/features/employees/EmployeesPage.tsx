import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Swal from "sweetalert2";
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
import type { EmployeeResponse } from "../../lib/types";
import { QueryError } from "../../components/QueryError";
import { EmptyState } from "../../ui/EmptyState";
import { SkeletonList } from "../../ui/Skeleton";
import { Button } from "../../ui/Button";
import { useToast } from "../../ui/Toast";

// Importando os subcomponentes organizados
import { EmployeeCard } from "./components/EmployeeCard";
import { EmployeeModal } from "./components/EmployeeModal";
import { EmployeeAccessDrawer } from "./components/EmployeeAccessDrawer";

const emptyForm = { jobTitleId: "", name: "", cpf: "", email: "", phone: "", salary: "" };
export type FormState = typeof emptyForm;

const emptyAccessForm = { hasSystemAccess: false, userName: "", userEmail: "", password: "" };
export type AccessFormState = typeof emptyAccessForm;

export type StatusFilter = "all" | "access" | "no-login";

const parseNum = (raw: string): number | null => {
    if (raw.trim() === "") return null;
    const value = Number(raw.replace(",", "."));
    return Number.isFinite(value) ? value : null;
};

const SearchIcon = () => (
    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <circle cx="11" cy="11" r="7" stroke="currentColor" strokeWidth="1.8" />
        <path d="M21 21l-4.3-4.3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
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

    const [access, setAccess] = useState<AccessFormState>(emptyAccessForm);
    const [extraFeatureIds, setExtraFeatureIds] = useState<number[]>([]);

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

    const cargoFeaturesQuery = useQuery({
        queryKey: ["access", "jobtitlefeatures", jobTitleIdNum],
        queryFn: () => getJobTitleFeatures(jobTitleIdNum as number),
        enabled: editing === "new" && access.hasSystemAccess && jobTitleIdNum !== null,
    });
    const cargoFeatureIds = useMemo(() => new Set(cargoFeaturesQuery.data ?? []), [cargoFeaturesQuery.data]);

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

    const onApiError = (e: unknown) => {
        const message = e instanceof ApiError ? e.message : "Operação falhou.";
        setError(message);
        Swal.fire({ title: "Atenção", text: message, icon: "error", confirmButtonText: "Ok" });
    };

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

    const toggleSystemAccess = (checked: boolean) =>
        setAccess((a) => ({
            ...a,
            hasSystemAccess: checked,
            userEmail: checked && a.userEmail === "" ? form.email.trim() : a.userEmail,
        }));

    const saveMutation = useMutation({
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
            Swal.fire({
                title: editing === "new" ? "Funcionário cadastrado!" : "Funcionário atualizado!",
                text: "Operação realizada com sucesso.",
                icon: "success",
                timer: 1500,
                showConfirmButton: false,
            });

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
            Swal.fire({
                title: "Demitido",
                text: "Funcionário demitido com sucesso.",
                icon: "success",
                timer: 1500,
                showConfirmButton: false,
            });
            refresh();
        },
        onError: onApiError,
    });

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
            Swal.fire({
                title: "Acessos atualizados!",
                text: "As permissões da pessoa foram salvas.",
                icon: "success",
                timer: 1500,
                showConfirmButton: false,
            });
            closeDrawer();
            refresh();
        },
        onError: onApiError,
    });

    const deactivateLoginMutation = useMutation({
        mutationFn: () => deactivateUser(accessDrawerFor!.appUserId as number),
        onSuccess: () => {
            Swal.fire({
                title: "Login desativado",
                text: "O funcionário continua na equipe, mas perdeu o acesso ao sistema.",
                icon: "success",
                timer: 1500,
                showConfirmButton: false,
            });
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
                <Button variant="primary" data-testid="btn-new-employee" onClick={() => openEditor("new")}>+ Novo funcionário</Button>
            </div>

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
                        <Button variant="primary" data-testid="btn-empty-new-employee" onClick={() => openEditor("new")}>
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
                <div className="emp-grid rise rise-1" data-testid="employees-grid">
                    {filteredEmployees.map((employee) => (
                        <EmployeeCard
                            key={employee.id}
                            employee={employee}
                            jobTitleName={jobTitleName}
                            isOpenKebab={openKebabFor === employee.id}
                            onToggleKebab={() => setOpenKebabFor(openKebabFor === employee.id ? null : employee.id)}
                            onCloseKebab={() => setOpenKebabFor(null)}
                            onEdit={() => openEditor(employee)}
                            onOpenAccess={() => {
                                setError(null);
                                setAccessDrawerFor(employee);
                            }}
                            onDismiss={async () => {
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
                        />
                    ))}
                </div>
            )}

            {editing !== null && (
                <EmployeeModal
                    editing={editing}
                    form={form}
                    setForm={setForm}
                    access={access}
                    setAccess={setAccess}
                    error={error}
                    creatingJobTitle={creatingJobTitle}
                    setCreatingJobTitle={setCreatingJobTitle}
                    newJobTitle={newJobTitle}
                    setNewJobTitle={setNewJobTitle}
                    jobTitles={jobTitlesQuery.data ?? []}
                    jobTitleName={jobTitleName}
                    jobTitleIdNum={jobTitleIdNum}
                    features={featuresQuery.data ?? []}
                    cargoFeatureIds={cargoFeatureIds}
                    extraFeatureIds={extraFeatureIds}
                    toggleSystemAccess={toggleSystemAccess}
                    toggleExtraFeature={toggleExtraFeature}
                    jobTitleMutation={jobTitleMutation}
                    saveMutation={saveMutation}
                    accessFieldsValid={accessFieldsValid}
                    onClose={() => setEditing(null)}
                />
            )}

            {accessDrawerFor !== null && (
                <EmployeeAccessDrawer
                    employee={accessDrawerFor}
                    jobTitleName={jobTitleName}
                    features={featuresQuery.data ?? []}
                    drawerCargoFeatureIds={drawerCargoFeatureIds}
                    drawerExtras={drawerExtras}
                    toggleDrawerExtra={toggleDrawerExtra}
                    drawerSaveMutation={drawerSaveMutation}
                    deactivateLoginMutation={deactivateLoginMutation}
                    isDrawerUserFeaturesLoading={drawerUserFeaturesQuery.isLoading}
                    error={error}
                    onClose={closeDrawer}
                    onConfirmDeactivate={async () => {
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
                />
            )}
        </main>
    );
}