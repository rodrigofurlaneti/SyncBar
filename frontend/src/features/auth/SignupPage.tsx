import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { registerCompany } from "./signupApi";
import { ApiError } from "../../lib/apiClient";

/**
 * Onboarding self-service: cadastra empresa + primeira filial + usuário administrador
 * em uma única chamada. Sem tela, o único jeito de criar um cliente novo era via SQL manual.
 */
export function SignupPage() {
  const navigate = useNavigate();
  const [form, setForm] = useState({
    legalName: "",
    tradeName: "",
    cnpj: "",
    branchName: "",
    adminUserName: "",
    adminEmail: "",
    adminPassword: "",
  });

  const set = (key: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm((f) => ({ ...f, [key]: e.target.value }));

  const mutation = useMutation({
    mutationFn: () => registerCompany(form),
    onSuccess: () => navigate("/login", { replace: true }),
  });

  const errorMessage =
    mutation.error instanceof ApiError
      ? mutation.error.message
      : mutation.isError
        ? "Não foi possível conectar à API."
        : null;

  return (
    <main style={{ minHeight: "100%", display: "grid", placeItems: "center", padding: 24 }}>
      <form
        className="rise"
        onSubmit={(e) => {
          e.preventDefault();
          mutation.mutate();
        }}
        style={{
          width: "min(440px, 100%)",
          background: "var(--bg-raise)",
          border: "1px solid var(--line)",
          borderRadius: 16,
          padding: "36px 32px 32px",
          display: "grid",
          gap: 14,
        }}
      >
        <div style={{ textAlign: "center", marginBottom: 8 }}>
          <div className="brand" style={{ fontSize: "2.4rem" }}>
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
            cadastre seu bar
          </div>
        </div>

        <label style={{ display: "grid", gap: 6 }}>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Razão social</span>
          <input value={form.legalName} onChange={set("legalName")} required autoFocus />
        </label>

        <label style={{ display: "grid", gap: 6 }}>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Nome fantasia</span>
          <input value={form.tradeName} onChange={set("tradeName")} required />
        </label>

        <label style={{ display: "grid", gap: 6 }}>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>CNPJ (só números)</span>
          <input value={form.cnpj} onChange={set("cnpj")} maxLength={14} required />
        </label>

        <label style={{ display: "grid", gap: 6 }}>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Nome da primeira filial</span>
          <input value={form.branchName} onChange={set("branchName")} required />
        </label>

        <hr style={{ border: "none", borderTop: "1px solid var(--line)", margin: "6px 0" }} />

        <label style={{ display: "grid", gap: 6 }}>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Usuário administrador</span>
          <input value={form.adminUserName} onChange={set("adminUserName")} autoComplete="username" required />
        </label>

        <label style={{ display: "grid", gap: 6 }}>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>E-mail do administrador</span>
          <input type="email" value={form.adminEmail} onChange={set("adminEmail")} autoComplete="email" required />
        </label>

        <label style={{ display: "grid", gap: 6 }}>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Senha</span>
          <input
            type="password"
            value={form.adminPassword}
            onChange={set("adminPassword")}
            autoComplete="new-password"
            minLength={8}
            required
          />
        </label>

        {errorMessage && <p className="error-text">{errorMessage}</p>}

        <button className="btn-primary" type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? "Criando conta…" : "Criar minha conta"}
        </button>

        <Link to="/login" style={{ textAlign: "center", color: "var(--ink-dim)", fontSize: "0.85rem" }}>
          Já tenho conta — entrar
        </Link>
      </form>
    </main>
  );
}
