import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { login } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import logoLight from "../../image/logoligth.png";
import bgImage from "../../image/screenbackground_auth.jpeg";

export function LoginPage() {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const setSession = useAuthStore((s) => s.setSession);
    const [userName, setUserName] = useState("");
    const [password, setPassword] = useState("");

    const commitHash = (
        import.meta.env?.VITE_COMMIT_HASH ||
        "Dev"
    ).substring(0, 7);

    const mutation = useMutation({
        mutationFn: () => login(userName, password),
        onSuccess: (session) => {
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
        <>
            <style>{`
                @keyframes fadeInAlpha {
                    from { opacity: 0; }
                    to { opacity: 1; }
                }

                /* Controle de exibição do logo baseado no tema global */
                .auth-logo-dark { display: block; }
                .auth-logo-light { display: none; }

                :root[data-theme="light"] .auth-logo-dark { display: none; }
                :root[data-theme="light"] .auth-logo-light { display: block; }
            `}</style>

            <main
                style={{
                    minHeight: "100vh",
                    display: "grid",
                    placeItems: "center",
                    padding: 24,
                    backgroundImage: `url(${bgImage})`,
                    backgroundSize: "cover",
                    backgroundPosition: "center",
                    animation: "fadeInAlpha 1s ease-out forwards",
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
                        boxShadow: "0 8px 32px rgba(0, 0, 0, 0.4)",
                    }}
                >
                    <div style={{ textAlign: "center", marginBottom: 8 }}>
                        {/* Logo Light - Aparece apenas no modo claro */}
                        <img
                            src={logoLight}
                            alt="Logo do Sistema (Light)"
                            className="auth-logo-light"
                            style={{
                                height: 100,
                                margin: "0 auto 16px",
                            }}
                        />

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

                    <Link to="/cadastro" style={{ textAlign: "center", color: "var(--ink-dim)", fontSize: "0.85rem" }}>
                        Ainda não tem conta? Cadastre seu bar
                    </Link>

                    {/* Rodapé com a tag de Versão dinâmica injetada no build */}
                    <div style={{ textAlign: "center", marginTop: 8, borderTop: "1px solid var(--line)", paddingTop: 12 }}>
                        <span style={{ color: "var(--ink-faint)", fontSize: "0.75rem", fontFamily: "monospace" }}>
                            Commit hash: {commitHash} - Release: 1.21v
                        </span>
                    </div>
                    <div style={{ textAlign: "center", marginTop: 8, borderTop: "1px solid var(--line)", paddingTop: 12 }}>
                        <span style={{ color: "var(--ink-faint)", fontSize: "0.75rem", fontFamily: "monospace" }}>
                            Last update: 2026-09-01
                        </span>
                    </div>
                </form>
            </main>
        </>
    );
}