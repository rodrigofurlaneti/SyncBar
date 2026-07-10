import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { login } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";

export function LoginPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const setSession = useAuthStore((s) => s.setSession);
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");

  const mutation = useMutation({
    mutationFn: () => login(userName, password),
    onSuccess: (session) => {
      // Nada do usuario anterior pode sobrar em cache (telas, listas, caixa...).
      queryClient.clear();
      setSession(session);
      navigate("/", { replace: true });
    },
  });

  const errorMessage =
    mutation.error instanceof ApiError
      ? mutation.error.message
      : mutation.isError
        ? "Não foi possível conectar à API."
        : null;

  return (
    <main
      style={{
        minHeight: "100%",
        display: "grid",
        placeItems: "center",
        padding: 24,
      }}
    >
      <form
        className="rise"
        onSubmit={(e) => {
          e.preventDefault();
          mutation.mutate();
        }}
        style={{
          width: "min(380px, 100%)",
          background: "var(--bg-raise)",
          border: "1px solid var(--line)",
          borderRadius: 16,
          padding: "36px 32px 32px",
          display: "grid",
          gap: 16,
        }}
      >
        <div style={{ textAlign: "center", marginBottom: 8 }}>
          <div className="brand" style={{ fontSize: "3rem" }}>
            SYNC<em>BAR</em>
          </div>
          <div
            style={{
              color: "var(--ink-faint)",
              fontFamily: "var(--font-cond)",
              letterSpacing: "0.22em",
              textTransform: "uppercase",
              fontSize: "0.78rem",
            }}
          >
            painel do salão
          </div>
        </div>

        <label style={{ display: "grid", gap: 6 }}>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Usuário</span>
          <input
            value={userName}
            onChange={(e) => setUserName(e.target.value)}
            autoComplete="username"
            autoFocus
            required
          />
        </label>

        <label style={{ display: "grid", gap: 6 }}>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Senha</span>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
            required
          />
        </label>

        {errorMessage && <p className="error-text">{errorMessage}</p>}

        <button className="btn-primary" type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? "Entrando…" : "Entrar"}
        </button>
      </form>
    </main>
  );
}
