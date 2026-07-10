import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createUser, deactivateUser, getRoles, getUsersByCompany, updateUserRoles } from "./api";
import { getEmployeesByBranch } from "../employees/api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import type { UserResponse } from "../../lib/types";
import { Overlay } from "../orders/Overlay";

export function UsersPage() {
  const queryClient = useQueryClient();
  const { companyId, branchId } = useAuthStore();
  const [creating, setCreating] = useState(false);
  const [editingRoles, setEditingRoles] = useState<UserResponse | null>(null);
  const [userName, setUserName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [employeeId, setEmployeeId] = useState("");
  const [selectedRoles, setSelectedRoles] = useState<number[]>([]);
  const [error, setError] = useState<string | null>(null);

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

  const onApiError = (e: unknown) =>
    setError(e instanceof ApiError ? e.message : "Operação falhou.");

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
    },
    onError: onApiError,
  });

  const rolesMutation = useMutation({
    mutationFn: () => updateUserRoles(editingRoles!.id, selectedRoles),
    onSuccess: () => {
      setEditingRoles(null);
      refresh();
    },
    onError: onApiError,
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: number) => deactivateUser(id),
    onSuccess: refresh,
    onError: onApiError,
  });

  const roleChecklist = (
    <div style={{ display: "grid", gap: 6 }}>
      <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Perfis</span>
      {(rolesQuery.data ?? []).map((role) => (
        <label key={role.id} style={{ display: "flex", gap: 10, alignItems: "center" }}>
          <input
            type="checkbox"
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
    </div>
  );

  return (
    <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }}>
      <div className="rise" style={{ display: "flex", alignItems: "baseline", gap: 14, marginBottom: 16 }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>Usuários e perfis</h2>
        <span style={{ flex: 1 }} />
        <button
          className="btn-primary"
          onClick={() => {
            setError(null);
            setSelectedRoles([]);
            setCreating(true);
          }}
        >
          + Novo usuário
        </button>
      </div>

      {error && !creating && editingRoles === null && <p className="error-text">{error}</p>}

      <div className="ticket rise rise-1">
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
                  className="btn-ghost"
                  style={{ minHeight: 38, padding: "0 12px", fontSize: "0.85rem" }}
                  onClick={() => {
                    setError(null);
                    setSelectedRoles(user.roleIds);
                    setEditingRoles(user);
                  }}
                >
                  Perfis
                </button>
                <button
                  className="btn-danger"
                  style={{ minHeight: 38, padding: "0 12px", fontSize: "0.85rem" }}
                  onClick={() => {
                    if (window.confirm(`Desativar o usuário "${user.userName}"?`))
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

      {creating && (
        <Overlay title="Novo usuário" onClose={() => setCreating(false)}>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Usuário</span>
            <input value={userName} onChange={(e) => setUserName(e.target.value)} />
          </label>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>E-mail</span>
            <input value={email} onChange={(e) => setEmail(e.target.value)} />
          </label>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Senha (mín. 8 caracteres)</span>
            <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
          </label>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Funcionário vinculado (opcional)</span>
            <select value={employeeId} onChange={(e) => setEmployeeId(e.target.value)}>
              <option value="">Nenhum</option>
              {(employeesQuery.data ?? []).map((emp) => (
                <option key={emp.id} value={emp.id}>{emp.name}</option>
              ))}
            </select>
          </label>
          {roleChecklist}
          {error && <p className="error-text">{error}</p>}
          <button
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
          {error && <p className="error-text">{error}</p>}
          <button
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
