import { useState } from "react";
import Swal from "sweetalert2";
import { api } from "../../lib/apiClient";

type StorefrontAuthModalProps = {
    isOpen: boolean;
    onClose: () => void;
    branchId: number;
    onAuthenticated: (customerData: { name: string; phone?: string; customerId?: number }) => void;
};

export function StorefrontAuthModal({
    isOpen,
    onClose,
    branchId,
    onAuthenticated,
}: StorefrontAuthModalProps) {
    const [mode, setMode] = useState<"login" | "register">("login");

    // Estados do Login
    const [loginEmail, setLoginEmail] = useState("");
    const [loginPassword, setLoginPassword] = useState("");

    // Estados do Cadastro (Novo Cliente + Endereço)
    const [regName, setRegName] = useState("");
    const [regPhone, setRegPhone] = useState("");
    const [regEmail, setRegEmail] = useState("");
    const [regPassword, setRegPassword] = useState("");

    // Campos de endereço exigidos no cadastro
    const [regStreet, setRegStreet] = useState("");
    const [regNumber, setRegNumber] = useState("");
    const [regSupplement, setRegSupplement] = useState("");
    const [regZipCode, setRegZipCode] = useState("");
    const [isLoading, setIsLoading] = useState(false);

    if (!isOpen) return null;

    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!loginEmail || !loginPassword) {
            Swal.fire("Atenção", "Preencha o e-mail e a senha.", "warning");
            return;
        }

        setIsLoading(true);
        try {
            const res = await api<any>(`/api/customerappusers/company/1`, { method: "GET" });
            const found = Array.isArray(res) ? res.find((u: any) => u.email.toLowerCase() === loginEmail.toLowerCase()) : null;

            if (!found) {
                throw new Error("E-mail não cadastrado como cliente.");
            }

            onAuthenticated({
                name: found.userName || loginEmail.split("@")[0],
                customerId: found.customerId
            });
            onClose();
        } catch (err: any) {
            Swal.fire("Erro", err.message || "Falha na autenticação.", "error");
        } finally {
            setIsLoading(false);
        }
    };

    const handleRegister = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!regName || !regEmail || !regPassword || !regStreet || !regNumber) {
            Swal.fire("Atenção", "Preencha os campos obrigatórios de cadastro e endereço (Rua e Número).", "warning");
            return;
        }

        setIsLoading(true);
        try {
            // 1. Cadastra o usuário e o cliente na API unificada (retorna o customerId corrigido)
            const userPayload = {
                companyId: 1,
                branchId: branchId,
                userName: regName,
                email: regEmail,
                password: regPassword,
                phone: regPhone || null,
            };

            const userResult = await api<{ id: number }>(`/api/customerappusers`, {
                method: "POST",
                body: JSON.stringify(userPayload),
            });

            const newCustomerId = userResult.id;

            // 2. Cadastra o endereço vinculado ao novo customerId gerado
            const addressPayload = {
                companyId: 1,
                branchId: branchId,
                customerId: newCustomerId,
                street: regStreet,
                number: regNumber,
                supplement: regSupplement || "",
            };

            await api(`/api/customeraddresses`, {
                method: "POST",
                body: JSON.stringify(addressPayload),
            });

            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: 'success',
                title: 'Cadastro e endereço realizados com sucesso!',
                showConfirmButton: false,
                timer: 2000,
                background: '#1e1e24',
                color: '#fff'
            });

            onAuthenticated({
                name: regName,
                phone: regPhone,
                customerId: newCustomerId
            });
            onClose();
        } catch (err: any) {
            Swal.fire("Erro", err.message || "Não foi possível realizar o cadastro.", "error");
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div data-testid="storefront-auth-modal" style={{ position: "fixed", inset: 0, backgroundColor: "rgba(0,0,0,0.85)", zIndex: 10000, display: "flex", alignItems: "center", justifyContent: "center", padding: 16, overflowY: "auto" }}>
            <div style={{ backgroundColor: "#1e1e24", padding: 32, borderRadius: 16, width: "100%", maxWidth: 480, border: "1px solid #323238", boxShadow: "0 10px 30px rgba(0,0,0,0.6)", display: "grid", gap: 20, maxHeight: "90vh", overflowY: "auto" }}>

                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                    <h3 style={{ margin: 0, color: "#fff", fontSize: "1.2rem" }}>
                        {mode === "login" ? "🔐 Identificação de Acesso" : "📝 Novo Cadastro e Endereço"}
                    </h3>
                    <button onClick={onClose} style={{ background: "none", border: "none", color: "#a8a8b3", fontSize: "1.5rem", cursor: "pointer" }}>✕</button>
                </div>

                {/* Abas / Modos */}
                <div style={{ display: "flex", gap: 8, backgroundColor: "#121214", padding: 4, borderRadius: 8 }}>
                    <button
                        type="button"
                        onClick={() => setMode("login")}
                        style={{ flex: 1, padding: 10, borderRadius: 6, border: "none", backgroundColor: mode === "login" ? "#f59e0b" : "transparent", color: mode === "login" ? "#121214" : "#a8a8b3", fontWeight: "bold", cursor: "pointer", fontSize: "0.85rem" }}
                    >
                        Já tenho cadastro
                    </button>
                    <button
                        type="button"
                        onClick={() => setMode("register")}
                        style={{ flex: 1, padding: 10, borderRadius: 6, border: "none", backgroundColor: mode === "register" ? "#f59e0b" : "transparent", color: mode === "register" ? "#121214" : "#a8a8b3", fontWeight: "bold", cursor: "pointer", fontSize: "0.85rem" }}
                    >
                        Novo Cliente
                    </button>
                </div>

                {mode === "login" ? (
                    <form onSubmit={handleLogin} style={{ display: "grid", gap: 12 }}>
                        <div style={{ display: "grid", gap: 6 }}>
                            <label style={{ fontSize: "0.85rem", color: "#a8a8b3" }}>E-mail</label>
                            <input
                                type="email"
                                value={loginEmail}
                                onChange={(e) => setLoginEmail(e.target.value)}
                                placeholder="seu@email.com"
                                style={{ width: "100%", padding: 12, borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", boxSizing: "border-box" }}
                                required
                            />
                        </div>
                        <div style={{ display: "grid", gap: 6 }}>
                            <label style={{ fontSize: "0.85rem", color: "#a8a8b3" }}>Senha</label>
                            <input
                                type="password"
                                value={loginPassword}
                                onChange={(e) => setLoginPassword(e.target.value)}
                                placeholder="******"
                                style={{ width: "100%", padding: 12, borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", boxSizing: "border-box" }}
                                required
                            />
                        </div>
                        <button
                            type="submit"
                            disabled={isLoading}
                            style={{ width: "100%", padding: 14, borderRadius: 8, border: "none", backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold", cursor: "pointer", marginTop: 8 }}
                        >
                            {isLoading ? "Entrando..." : "Entrar e Finalizar Pedido"}
                        </button>
                    </form>
                ) : (
                    <form onSubmit={handleRegister} style={{ display: "grid", gap: 12 }}>
                        <div style={{ display: "grid", gap: 6 }}>
                            <label style={{ fontSize: "0.85rem", color: "#a8a8b3" }}>Nome Completo</label>
                            <input
                                type="text"
                                value={regName}
                                onChange={(e) => setRegName(e.target.value)}
                                placeholder="Seu nome"
                                style={{ width: "100%", padding: 10, borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", boxSizing: "border-box" }}
                                required
                            />
                        </div>
                        <div style={{ display: "grid", gap: 6 }}>
                            <label style={{ fontSize: "0.85rem", color: "#a8a8b3" }}>Telefone / WhatsApp</label>
                            <input
                                type="text"
                                value={regPhone}
                                onChange={(e) => setRegPhone(e.target.value)}
                                placeholder="(11) 99999-9999"
                                style={{ width: "100%", padding: 10, borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", boxSizing: "border-box" }}
                            />
                        </div>
                        <div style={{ display: "grid", gap: 6 }}>
                            <label style={{ fontSize: "0.85rem", color: "#a8a8b3" }}>E-mail</label>
                            <input
                                type="email"
                                value={regEmail}
                                onChange={(e) => setRegEmail(e.target.value)}
                                placeholder="seu@email.com"
                                style={{ width: "100%", padding: 10, borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", boxSizing: "border-box" }}
                                required
                            />
                        </div>
                        <div style={{ display: "grid", gap: 6 }}>
                            <label style={{ fontSize: "0.85rem", color: "#a8a8b3" }}>Senha</label>
                            <input
                                type="password"
                                value={regPassword}
                                onChange={(e) => setRegPassword(e.target.value)}
                                placeholder="Mínimo 6 caracteres"
                                style={{ width: "100%", padding: 10, borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", boxSizing: "border-box" }}
                                required
                            />
                        </div>

                        {/* Seção de Endereço */}
                        <div style={{ borderTop: "1px solid #323238", paddingTop: 10, marginTop: 4, display: "grid", gap: 10 }}>
                            <span style={{ fontSize: "0.9rem", fontWeight: "bold", color: "#f59e0b" }}>Endereço de Entrega</span>
                            <div style={{ display: "grid", gridTemplateColumns: "3fr 1fr", gap: 8 }}>
                                <div style={{ display: "grid", gap: 4 }}>
                                    <label style={{ fontSize: "0.8rem", color: "#a8a8b3" }}>Cep</label>
                                        <input
                                            type="text"
                                            value={regZipCode}
                                            onChange={(e) => setRegZipCode(e.target.value)}
                                            placeholder="00000000"
                                            style={{ width: "100%", padding: 10, borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", boxSizing: "border-box", maxHeight: 9 }}
                                            required
                                    />
                                </div>

                                <div style={{ display: "grid", gap: 4 }}>
                                    <label style={{ fontSize: "0.8rem", color: "#a8a8b3" }}>Rua / Avenida</label>
                                    <input
                                        type="text"
                                        value={regStreet}
                                        onChange={(e) => setRegStreet(e.target.value)}
                                        placeholder="Nome da rua"
                                        style={{ width: "100%", padding: 10, borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", boxSizing: "border-box" }}
                                        required
                                    />
                                </div>
                                <div style={{ display: "grid", gap: 4 }}>
                                    <label style={{ fontSize: "0.8rem", color: "#a8a8b3" }}>Número</label>
                                    <input
                                        type="text"
                                        value={regNumber}
                                        onChange={(e) => setRegNumber(e.target.value)}
                                        placeholder="123"
                                        style={{ width: "100%", padding: 10, borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", boxSizing: "border-box" }}
                                        required
                                    />
                                </div>
                            </div>
                            <div style={{ display: "grid", gap: 4 }}>
                                <label style={{ fontSize: "0.8rem", color: "#a8a8b3" }}>Complemento / Bairro</label>
                                <input
                                    type="text"
                                    value={regSupplement}
                                    onChange={(e) => setRegSupplement(e.target.value)}
                                    placeholder="Apto 42, Bloco B"
                                    style={{ width: "100%", padding: 10, borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", boxSizing: "border-box" }}
                                />
                            </div>
                        </div>

                        <button
                            type="submit"
                            disabled={isLoading}
                            style={{ width: "100%", padding: 14, borderRadius: 8, border: "none", backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold", cursor: "pointer", marginTop: 8 }}
                        >
                            {isLoading ? "Cadastrando..." : "Cadastrar e Enviar Pedido"}
                        </button>
                    </form>
                )}
            </div>
        </div>
    );
}