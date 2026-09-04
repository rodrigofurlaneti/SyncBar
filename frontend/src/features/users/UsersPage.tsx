import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Swal from "sweetalert2";
import { useDialog } from "../../ui/Dialog";
import { createRole, createUser, deactivateUser, getRoles, getUsersByCompany, updateUserRoles } from "./api";
import { getEmployeesByBranch } from "../employees/api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import type { UserResponse } from "../../lib/types";
import { Overlay } from "../orders/Overlay";
import { QueryError } from "../../components/QueryError";
import { EmptyState } from "../../ui/EmptyState";
import { SkeletonList } from "../../ui/Skeleton";

export function UsersPage() {
    const queryClient = useQueryClient();
    const dialog = useDialog();
    const { companyId, branchId } = useAuthStore();
    const [creating, setCreating] = useState(false);
    const [editingRoles, setEditingRoles] = useState<UserResponse | null>(null);
    const [userName, setUserName] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [employeeId, setEmployeeId] = useState("");
    const [selectedRoles, setSelectedRoles] = useState<number[]>([]);
    const [newRoleName, setNewRoleName] = useState("");
    const [newRoleDescription, setNewRoleDescription] = useState("");

    const usersQuery = useQuery({
        queryKey: ["users", companyId],
        queryFn: () => getUsersByCompany(companyId ?? 1),
    });

    const rolesQuery = useQuery({
        queryKey: ["roles", companyId],
        queryFn: () => getRoles(companyId ?? 1),
    });

    const employeesQuery = useQuery({
        queryKey: ["employees", branchId],
        queryFn: () => getEmployeesByBranch(branchId),
    });

    const roleName = useMemo(() => {
        const map = new Map<number, string>();
        for (const r of rolesQuery.data ?? []) map.set(r.id, r.name);
        return map;
    }, [rolesQuery.data]);

    const refresh = () => void queryClient.invalidateQueries({ queryKey: ["users"] });

    const onApiError = (e: unknown) => {
        const message = e instanceof ApiError ? e.message : "Operação falhou.";
        Swal.fire({
            title: "Atenção",
            text: message,
            icon: "error",
            confirmButtonText: "Ok",
        });
    };

    const toggleRole = (roleId: number) =>
        setSelectedRoles((current) =>
            current.includes(roleId) ? current.filter((id) => id !== roleId) : [...current, roleId],
        );

    const createMutation = useMutation({
        mutationFn: () =>
            createUser({
                companyId: companyId ?? 1,
                employeeId: employeeId === "" ? null : Number(employeeId),
                userName: userName.trim(),
                email: email.trim(),
                password,
                roleIds: selectedRoles,
            }),
        onSuccess: () => {
            setCreating(false);
            setUserName(""); setEmail(""); setPassword(""); setEmployeeId(""); setSelectedRoles([]);
            refresh();
            Swal.fire({
                title: "Sucesso!",
                text: "Usuário criado com sucesso.",
                icon: "success",
                timer: 1500,
                showConfirmButton: false,
            });
        },
        onError: onApiError,
    });

    const rolesMutation = useMutation({
        mutationFn: () => updateUserRoles(editingRoles!.id, selectedRoles),
        onSuccess: () => {
            setEditingRoles(null);
            refresh();
            Swal.fire({
                title: "Sucesso!",
                text: "Perfis atualizados com sucesso.",
                icon: "success",
                timer: 1500,
                showConfirmButton: false,
            });
        },
        onError: onApiError,
    });

    const deactivateMutation = useMutation({
        mutationFn: (id: number) => deactivateUser(id),
        onSuccess: () => {
            refresh();
            Swal.fire({
                title: "Desativado",
                text: "O usuário foi desativado.",
                icon: "success",
                timer: 1500,
                showConfirmButton: false,
            });
        },
        onError: onApiError,
    });

    const createRoleMutation = useMutation({
        mutationFn: () =>
            createRole({
                companyId: companyId ?? 1,
                name: newRoleName.trim(),
                description: newRoleDescription.trim() === "" ? null : newRoleDescription.trim(),
            }),
        onSuccess: async (newRoleId) => {
            setNewRoleName("");
            setNewRoleDescription("");
            await queryClient.invalidateQueries({ queryKey: ["roles"] });
            setSelectedRoles((current) => (current.includes(newRoleId) ? current : [...current, newRoleId]));
        },
        onError: onApiError,
    });

    const roleChecklist = (
        <div style={{ display: "grid", gap: 6 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Perfis</span>
            {(rolesQuery.data ?? []).map((role) => (
                <label key={role.id} style={{ display: "flex", gap: 10, alignItems: "center" }}>
                    <input
                        type="checkbox"
                        data-testid={`role-checkbox-${role.id}`}
                        style={{ width: 20, minHeight: 20 }}
                        checked={selectedRoles.includes(role.id)}
                        onChange={() => toggleRole(role.id)}
                    />
                    <span>
                        {role.name}
                        {role.description ? (
                            <span style={{ color: "var(--ink-faint)", fontSize: "0.8rem" }}> — {role.description}</span>
                        ) : null}
                    </span>
                </label>
            ))}

            <div
                style={{
                    display: "grid",
                    gap: 6,
                    marginTop: 4,
                    paddingTop: 10,
                    borderTop: "1px solid var(--line)",
                }}
            >
                <span style={{ color: "var(--ink-faint)", fontSize: "0.8rem" }}>
                    Não achou o perfil que precisa? Crie um novo abaixo (ex.: Garçom, Cozinha) — ele já
                    aparece marcado na lista acima.
                </span>
                <div style={{ display: "flex", gap: 8 }}>
                    <input
                        data-testid="new-role-name"
                        placeholder="Nome do novo perfil"
                        value={newRoleName}
                        onChange={(e) => setNewRoleName(e.target.value)}
                        style={{ flex: 1 }}
                    />
                    <input
                        data-testid="new-role-description"
                        placeholder="Descrição (opcional)"
                        value={newRoleDescription}
                        onChange={(e) => setNewRoleDescription(e.target.value)}
                        style={{ flex: 1 }}
                    />
                    <button
                        type="button"
                        data-testid="btn-create-role"
                        className="btn-ghost"
                        style={{ minHeight: 44, padding: "0 14px", fontSize: "0.85rem", whiteSpace: "nowrap" }}
                        disabled={newRoleName.trim() === "" || createRoleMutation.isPending}
                        onClick={() => createRoleMutation.mutate()}
                    >
                        {createRoleMutation.isPending ? "Criando…" : "+ Novo perfil"}
                    </button>
                </div>
            </div>
        </div>
    );

    return (
        <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }}>
            <div className="rise" style={{ display: "flex", alignItems: "baseline", gap: 14, marginBottom: 16 }}>
                <h2 className="display" style={{ fontSize: "1.7rem" }}>Usuários e perfis</h2>
                <span style={{ flex: 1 }} />
                <button
                    type="button"
                    data-testid="btn-new-user"
                    className="btn-primary"
                    onClick={() => {
                        setSelectedRoles([]);
                        setCreating(true);
                    }}
                >
                    + Novo usuário
                </button>
            </div>

            {usersQuery.isError && <QueryError error={usersQuery.error} what="os usuários" />}
            {rolesQuery.isError && <QueryError error={rolesQuery.error} what="os perfis" />}

            {usersQuery.isLoading && <SkeletonList rows={5} rowHeight={58} />}

            {!usersQuery.isLoading && usersQuery.data?.length === 0 && (
                <EmptyState
                    icon="👤"
                    title="Nenhum usuário cadastrado"
                    description="Crie o primeiro usuário para dar acesso ao sistema à equipe."
                    action={
                        <button
                            type="button"
                            data-testid="btn-empty-new-user"
                            className="btn-primary"
                            onClick={() => {
                                setSelectedRoles([]);
                                setCreating(true);
                            }}
                        >
                            + Novo usuário
                        </button>
                    }
                />
            )}

            {!usersQuery.isLoading && (usersQuery.data?.length ?? 0) > 0 && (
                <div className="ticket rise rise-1" data-testid="users-list">
                    {(usersQuery.data ?? []).map((user) => (
                        <div className="ticket-row" key={user.id} style={{ opacity: user.isActive ? 1 : 0.45 }}>
                            <div style={{ display: "grid", gap: 2 }}>
                                <span>
                                    {user.userName}
                                    {!user.isActive && <span style={{ color: "var(--danger)" }}> · desativado</span>}
                                </span>
                                <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                                    {user.email}
                                    {" · "}
                                    {user.roleIds.length > 0
                                        ? user.roleIds.map((id) => roleName.get(id) ?? id).join(", ")
                                        : "sem perfil"}
                                </span>
                            </div>
                            {user.isActive && (
                                <div style={{ display: "flex", gap: 8 }}>
                                    <button
                                        type="button"
                                        data-testid={`btn-edit-roles-${user.id}`}
                                        className="btn-ghost"
                                        style={{ minHeight: 44, padding: "0 12px", fontSize: "0.85rem" }}
                                        onClick={() => {
                                            setSelectedRoles(user.roleIds);
                                            setEditingRoles(user);
                                        }}
                                    >
                                        Perfis
                                    </button>
                                    <button
                                        type="button"
                                        data-testid={`btn-deactivate-${user.id}`}
                                        className="btn-danger"
                                        style={{ minHeight: 44, padding: "0 12px", fontSize: "0.85rem" }}
                                        onClick={async () => {
                                            if (
                                                await dialog.confirm({
                                                    title: "Desativar usuário",
                                                    message: `Desativar o usuário "${user.userName}"?`,
                                                    confirmLabel: "Desativar",
                                                    danger: true,
                                                })
                                            )
                                                deactivateMutation.mutate(user.id);
                                        }}
                                    >
                                        Desativar
                                    </button>
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            )}

            {creating && (
                <Overlay title="Novo usuário" onClose={() => setCreating(false)}>
                    <label style={{ display: "grid", gap: 4 }}>
                        <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Usuário</span>
                        <input data-testid="input-username" value={userName} onChange={(e) => setUserName(e.target.value)} />
                    </label>
                    <label style={{ display: "grid", gap: 4 }}>
                        <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>E-mail</span>
                        <input data-testid="input-email" value={email} onChange={(e) => setEmail(e.target.value)} />
                    </label>
                    <label style={{ display: "grid", gap: 4 }}>
                        <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Senha (mín. 8 caracteres)</span>
                        <input data-testid="input-password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
                    </label>
                    <label style={{ display: "grid", gap: 4 }}>
                        <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Funcionário vinculado (opcional)</span>
                        <select data-testid="select-employee" value={employeeId} onChange={(e) => setEmployeeId(e.target.value)}>
                            <option value="">Nenhum</option>
                            {(employeesQuery.data ?? []).map((emp) => (
                                <option key={emp.id} value={emp.id}>{emp.name}</option>
                            ))}
                        </select>
                    </label>
                    {roleChecklist}
                    <button
                        type="button"
                        data-testid="btn-submit-user"
                        className="btn-primary"
                        disabled={
                            userName.trim() === "" || email.trim() === "" || password.length < 8 ||
                            selectedRoles.length === 0 || createMutation.isPending
                        }
                        onClick={() => createMutation.mutate()}
                    >
                        {createMutation.isPending ? "Criando…" : "Criar usuário"}
                    </button>
                </Overlay>
            )}

            {editingRoles !== null && (
                <Overlay title={`Perfis — ${editingRoles.userName}`} onClose={() => setEditingRoles(null)}>
                    {roleChecklist}
                    <button
                        type="button"
                        data-testid="btn-save-roles"
                        className="btn-primary"
                        disabled={selectedRoles.length === 0 || rolesMutation.isPending}
                        onClick={() => rolesMutation.mutate()}
                    >
                        Salvar perfis
                    </button>
                </Overlay>
            )}
        </main>
    );
}