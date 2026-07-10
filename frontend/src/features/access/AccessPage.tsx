import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  getFeatures,
  getJobTitleFeatures,
  getUserFeatures,
  setJobTitleFeatures,
  setUserFeatures,
} from "./api";
import { getJobTitles } from "../employees/api";
import { getUsersByCompany } from "../users/api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { QueryError } from "../../components/QueryError";

type Mode = "cargo" | "pessoa";

export function AccessPage() {
  const queryClient = useQueryClient();
  const { companyId } = useAuthStore();
  const [mode, setMode] = useState<Mode>("cargo");
  const [targetId, setTargetId] = useState<string>("");
  const [selected, setSelected] = useState<number[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  const featuresQuery = useQuery({ queryKey: ["access", "features"], queryFn: getFeatures });

  const jobTitlesQuery = useQuery({
    queryKey: ["jobtitles", companyId],
    queryFn: () => getJobTitles(companyId ?? 1),
    enabled: mode === "cargo",
  });

  const usersQuery = useQuery({
    queryKey: ["users", companyId],
    queryFn: () => getUsersByCompany(companyId ?? 1),
    enabled: mode === "pessoa",
  });

  const grantsQuery = useQuery({
    queryKey: ["access", mode, targetId],
    queryFn: () =>
      mode === "cargo" ? getJobTitleFeatures(Number(targetId)) : getUserFeatures(Number(targetId)),
    enabled: targetId !== "",
  });

  // Sincroniza os checkboxes quando os grants chegam
  const grants = grantsQuery.data;
  const [loadedFor, setLoadedFor] = useState<string>("");
  if (grants !== undefined && loadedFor !== `${mode}:${targetId}`) {
    setSelected(grants);
    setLoadedFor(`${mode}:${targetId}`);
  }

  const saveMutation = useMutation({
    mutationFn: () =>
      mode === "cargo"
        ? setJobTitleFeatures(Number(targetId), selected)
        : setUserFeatures(Number(targetId), selected),
    onSuccess: () => {
      setError(null);
      setSaved(true);
      setTimeout(() => setSaved(false), 2500);
      void queryClient.invalidateQueries({ queryKey: ["access"] });
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "Falha ao salvar."),
  });

  const switchMode = (next: Mode) => {
    setMode(next);
    setTargetId("");
    setSelected([]);
    setLoadedFor("");
    setError(null);
  };

  const toggle = (featureId: number) =>
    setSelected((current) =>
      current.includes(featureId) ? current.filter((id) => id !== featureId) : [...current, featureId],
    );

  return (
    <main style={{ padding: 22, maxWidth: 760, margin: "0 auto" }}>
      <div className="rise" style={{ display: "grid", gap: 6, marginBottom: 18 }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>Acessos</h2>
        <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
          O acesso efetivo de cada pessoa é a união do que o cargo dá com o que foi
          concedido individualmente. Gerente e Administrador sempre veem tudo.
        </span>
      </div>

      <div className="rise rise-1" style={{ display: "flex", gap: 8, marginBottom: 14 }}>
        <button
          className={mode === "cargo" ? "btn-primary" : "btn-ghost"}
          onClick={() => switchMode("cargo")}
        >
          Por cargo
        </button>
        <button
          className={mode === "pessoa" ? "btn-primary" : "btn-ghost"}
          onClick={() => switchMode("pessoa")}
        >
          Por pessoa
        </button>
      </div>

      {featuresQuery.isError && <QueryError error={featuresQuery.error} what="as telas" />}
      {jobTitlesQuery.isError && <QueryError error={jobTitlesQuery.error} what="os cargos" />}
      {usersQuery.isError && <QueryError error={usersQuery.error} what="os usuários" />}
      {grantsQuery.isError && <QueryError error={grantsQuery.error} what="os acessos atuais" />}

      <div className="rise rise-2" style={{ display: "grid", gap: 14 }}>
        <select value={targetId} onChange={(e) => { setTargetId(e.target.value); setLoadedFor(""); }}>
          <option value="">{mode === "cargo" ? "Selecione o cargo…" : "Selecione a pessoa…"}</option>
          {mode === "cargo"
            ? (jobTitlesQuery.data ?? []).map((j) => (
                <option key={j.id} value={j.id}>{j.name}</option>
              ))
            : (usersQuery.data ?? [])
                .filter((u) => u.isActive)
                .map((u) => (
                  <option key={u.id} value={u.id}>{u.userName} ({u.email})</option>
                ))}
        </select>

        {targetId !== "" && (
          <div className="ticket" style={{ padding: 18, display: "grid", gap: 10 }}>
            {(featuresQuery.data ?? []).map((feature) => (
              <label key={feature.id} style={{ display: "flex", gap: 12, alignItems: "center" }}>
                <input
                  type="checkbox"
                  style={{ width: 22, minHeight: 22 }}
                  checked={selected.includes(feature.id)}
                  onChange={() => toggle(feature.id)}
                />
                <span>{feature.name}</span>
              </label>
            ))}
            {mode === "pessoa" && (
              <span style={{ color: "var(--ink-faint)", fontSize: "0.82rem" }}>
                Marque apenas os EXTRAS desta pessoa — o que vem do cargo já está garantido.
              </span>
            )}
            {error && <p className="error-text">{error}</p>}
            {saved && <p style={{ color: "var(--ok)", margin: 0 }}>Acessos salvos.</p>}
            <button
              className="btn-primary"
              disabled={saveMutation.isPending || grantsQuery.isLoading}
              onClick={() => saveMutation.mutate()}
            >
              {saveMutation.isPending ? "Salvando…" : "Salvar acessos"}
            </button>
          </div>
        )}
      </div>
    </main>
  );
}
