import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import Swal from "sweetalert2";
import { registerCompany } from "./signupApi";
import { ApiError } from "../../lib/apiClient";

export function SignupPage() {
    const navigate = useNavigate();
    const [form, setForm] = useState({
        legalName: "",
        tradeName: "",
        cpf: "",
        cnpj: "",
        branchName: "",
        adminName: "",
        adminCpf: "",
        adminUserName: "",
        adminEmail: "",
        adminPassword: "",
    });

    const set = (key: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) =>
        setForm((f) => ({ ...f, [key]: e.target.value }));

    const mutation = useMutation({
        mutationFn: () => registerCompany(form),
        onSuccess: () => {
            // Feedback de Sucesso com SweetAlert
            Swal.fire({
                title: "Conta criada!",
                text: "Seu bar foi cadastrado com sucesso.",
                icon: "success",
                timer: 1500, // Fecha sozinho após 1.5s
                showConfirmButton: false,
            }).then(() => {
                navigate("/login", { replace: true });
            });
        },
        onError: (error) => {
            const message = error instanceof ApiError
                ? error.message
                : "Não foi possível conectar à API.";

            // Feedback de Erro com SweetAlert
            Swal.fire({
                title: "Erro no Cadastro",
                text: message,
                icon: "error",
                confirmButtonText: "Revisar dados",
                confirmButtonColor: "var(--primary)",
            });
        }
    });

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

                <label htmlFor="legalName" style={{ display: "grid", gap: 6 }}>
                    <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Razão social</span>
                    <input
                        id="legalName"
                        data-testid="legalName"
                        value={form.legalName}
                        onChange={set("legalName")}
                        required
                        autoFocus
                    />
                </label>

                <label htmlFor="tradeName" style={{ display: "grid", gap: 6 }}>
                    <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Nome fantasia</span>
                    <input
                        id="tradeName"
                        data-testid="tradeName"
                        value={form.tradeName}
                        onChange={set("tradeName")}
                        required
                    />
                </label>

                <label htmlFor="cnpj" style={{ display: "grid", gap: 6 }}>
                    <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>CNPJ (só números)</span>
                    <input
                        id="cnpj"
                        data-testid="cnpj"
                        value={form.cnpj}
                        onChange={set("cnpj")}
                        maxLength={14}
                        required
                    />
                </label>

                <label htmlFor="adminName" style={{ display: "grid", gap: 6 }}>
                    <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Nome completo do administrador</span>
                    <input
                        id="adminName"
                        data-testid="adminName"
                        value={form.adminName}
                        onChange={set("adminName")}
                        required
                    />
                </label>

                <label htmlFor="adminCpf" style={{ display: "grid", gap: 6 }}>
                    <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>CPF do administrador</span>
                    <input
                        id="adminCpf"
                        data-testid="adminCpf"
                        value={form.adminCpf}
                        onChange={set("adminCpf")}
                        maxLength={11}
                        required
                    />
                </label>

                <label htmlFor="branchName" style={{ display: "grid", gap: 6 }}>
                    <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Nome da primeira filial</span>
                    <input
                        id="branchName"
                        data-testid="branchName"
                        value={form.branchName}
                        onChange={set("branchName")}
                        required
                    />
                </label>

                <hr style={{ border: "none", borderTop: "1px solid var(--line)", margin: "6px 0" }} />

                <label htmlFor="adminUserName" style={{ display: "grid", gap: 6 }}>
                    <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Usuário administrador</span>
                    <input
                        id="adminUserName"
                        data-testid="adminUserName"
                        value={form.adminUserName}
                        onChange={set("adminUserName")}
                        autoComplete="username"
                        required
                    />
                </label>

                <label htmlFor="adminEmail" style={{ display: "grid", gap: 6 }}>
                    <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>E-mail do administrador</span>
                    <input
                        id="adminEmail"
                        data-testid="adminEmail"
                        type="email"
                        value={form.adminEmail}
                        onChange={set("adminEmail")}
                        autoComplete="email"
                        required
                    />
                </label>

                <label htmlFor="adminPassword" style={{ display: "grid", gap: 6 }}>
                    <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Senha</span>
                    <input
                        id="adminPassword"
                        data-testid="adminPassword"
                        type="password"
                        value={form.adminPassword}
                        onChange={set("adminPassword")}
                        autoComplete="new-password"
                        minLength={8}
                        required
                    />
                </label>

                <button
                    className="btn-primary"
                    type="submit"
                    disabled={mutation.isPending}
                    data-testid="submit-signup"
                >
                    {mutation.isPending ? "Criando conta…" : "Criar minha conta"}
                </button>

                <Link to="/login" style={{ textAlign: "center", color: "var(--ink-dim)", fontSize: "0.85rem" }}>
                    Já tenho conta — entrar
                </Link>
            </form>
        </main>
    );
}