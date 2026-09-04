import { useState, useId, useEffect } from "react";
import Swal from "sweetalert2";
import { api } from "../../lib/apiClient";
import { loginCustomerAppUser } from "./storefrontApi"; // Adicionado a importação do novo endpoint

type StorefrontAuthModalProps = {
    isOpen: boolean;
    onClose: () => void;
    branchId: number;
    onAuthenticated: (customerData: { name: string; phone?: string; customerId?: number }) => void;
};

// COMPONENTE MOVIDO PARA FORA: Evita a perda de foco na digitação
const InputField = ({ label, id, isLoading, ...props }: any) => (
    <div style={{ display: "flex", flexDirection: "column", gap: "6px", width: "100%" }}>
        <label htmlFor={id} style={{ fontSize: "0.875rem", fontWeight: 500, color: "#a1a1aa" }}>
            {label}
        </label>
        <input
            id={id}
            disabled={isLoading}
            style={{
                width: "100%",
                borderRadius: "8px",
                border: "1px solid #3f3f46",
                backgroundColor: "#09090b",
                padding: "10px 16px",
                color: "#f4f4f5",
                boxSizing: "border-box",
                outline: "none",
                opacity: isLoading ? 0.5 : 1,
                cursor: isLoading ? "not-allowed" : "text",
                fontSize: "0.95rem"
            }}
            {...props}
        />
    </div>
);

export function StorefrontAuthModal({
    isOpen,
    onClose,
    branchId,
    onAuthenticated,
}: StorefrontAuthModalProps) {
    const [mode, setMode] = useState<"login" | "register">("login");
    const [isLoading, setIsLoading] = useState(false);

    // Controle de responsividade básico
    const [windowWidth, setWindowWidth] = useState(typeof window !== "undefined" ? window.innerWidth : 1200);
    useEffect(() => {
        const handleResize = () => setWindowWidth(window.innerWidth);
        window.addEventListener("resize", handleResize);
        return () => window.removeEventListener("resize", handleResize);
    }, []);
    const isMobile = windowWidth < 640;

    // Gerador de IDs únicos para acessibilidade
    const formId = useId();

    // Estados do Login
    const [loginEmail, setLoginEmail] = useState("");
    const [loginPassword, setLoginPassword] = useState("");

    // Estados do Cadastro
    const [regName, setRegName] = useState("");
    const [regPhone, setRegPhone] = useState("");
    const [regEmail, setRegEmail] = useState("");
    const [regPassword, setRegPassword] = useState("");
    const [regZipCode, setRegZipCode] = useState("");
    const [regStreet, setRegStreet] = useState("");
    const [regNumber, setRegNumber] = useState("");
    const [regSupplement, setRegSupplement] = useState("");

    if (!isOpen) return null;

    // NOVO FLUXO DE LOGIN
    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!loginEmail || !loginPassword) {
            Swal.fire("Atenção", "Preencha o e-mail e a senha.", "warning");
            return;
        }

        setIsLoading(true);
        try {
            const payload = {
                email: loginEmail,
                password: loginPassword,
                companyId: 1, // Substitua dinamicamente se necessário
                branchId: branchId
            };

            const res = await loginCustomerAppUser(payload);

            // Se desejar persistir a sessão do cliente, salve o token aqui:
            // localStorage.setItem("customerToken", res.accessToken);

            onAuthenticated({
                name: res.userName,
                customerId: res.customerId
            });
            onClose();
        } catch (err: any) {
            Swal.fire("Erro", err.message || "E-mail ou senha incorretos.", "error");
        } finally {
            setIsLoading(false);
        }
    };

    const handleRegister = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!regName || !regEmail || !regPassword || !regStreet || !regNumber || !regZipCode) {
            Swal.fire("Atenção", "Preencha os campos obrigatórios de cadastro e endereço (incluindo CEP).", "warning");
            return;
        }

        setIsLoading(true);
        try {
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

            const addressPayload = {
                companyId: 1,
                branchId: branchId,
                customerId: newCustomerId,
                street: regStreet,
                number: regNumber,
                supplement: regSupplement || "",
                zipCode: regZipCode,
            };

            await api(`/api/customeraddresses`, {
                method: "POST",
                body: JSON.stringify(addressPayload),
            });

            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: 'success',
                title: 'Cadastro realizado com sucesso!',
                showConfirmButton: false,
                timer: 2000,
                background: '#18181b',
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
        <div
            data-testid="storefront-auth-modal"
            style={{ position: "fixed", inset: 0, zIndex: 10000, display: "flex", alignItems: "center", justifyItems: "center", padding: isMobile ? "16px" : "24px", overflowY: "auto", fontFamily: "sans-serif" }}
            aria-modal="true"
            role="dialog"
            onClick={onClose}
        >
            {/* Overlay Escurecido */}
            <div style={{ position: "absolute", inset: 0, backgroundColor: "rgba(0,0,0,0.8)", backdropFilter: "blur(4px)" }} aria-hidden="true" />

            <div
                onClick={(e) => e.stopPropagation()}
                style={{ position: "relative", width: "100%", maxWidth: "440px", margin: "auto", display: "flex", flexDirection: "column", gap: "24px", borderRadius: "16px", border: "1px solid #27272a", backgroundColor: "#18181b", padding: isMobile ? "24px" : "32px", boxShadow: "0 25px 50px -12px rgba(0,0,0,0.7)" }}
            >
                {/* Cabeçalho */}
                <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                    <h3 style={{ margin: 0, fontSize: "1.125rem", fontWeight: 600, color: "#f4f4f5" }}>
                        {mode === "login" ? "🔐 Identificação" : "📝 Novo Cadastro"}
                    </h3>
                    <button
                        onClick={onClose}
                        aria-label="Fechar modal"
                        disabled={isLoading}
                        style={{ background: "none", border: "none", padding: "4px", color: "#a1a1aa", cursor: isLoading ? "not-allowed" : "pointer", borderRadius: "6px" }}
                    >
                        <svg style={{ height: "24px", width: "24px" }} fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                    </button>
                </div>

                {/* Abas Alternadoras */}
                <div style={{ display: "flex", gap: "4px", borderRadius: "8px", backgroundColor: "#09090b", padding: "4px" }}>
                    <button
                        type="button"
                        onClick={() => setMode("login")}
                        disabled={isLoading}
                        style={{ flex: 1, padding: "10px", borderRadius: "6px", border: "none", fontWeight: 600, fontSize: "0.875rem", cursor: isLoading ? "not-allowed" : "pointer", backgroundColor: mode === "login" ? "#f59e0b" : "transparent", color: mode === "login" ? "#18181b" : "#a1a1aa", transition: "all 0.2s" }}
                    >
                        Já tenho cadastro
                    </button>
                    <button
                        type="button"
                        onClick={() => setMode("register")}
                        disabled={isLoading}
                        style={{ flex: 1, padding: "10px", borderRadius: "6px", border: "none", fontWeight: 600, fontSize: "0.875rem", cursor: isLoading ? "not-allowed" : "pointer", backgroundColor: mode === "register" ? "#f59e0b" : "transparent", color: mode === "register" ? "#18181b" : "#a1a1aa", transition: "all 0.2s" }}
                    >
                        Novo Cliente
                    </button>
                </div>

                {/* Formulários */}
                {mode === "login" ? (
                    <form onSubmit={handleLogin} style={{ display: "flex", flexDirection: "column", gap: "20px" }}>
                        <InputField label="E-mail" id={`${formId}-login-email`} type="email" placeholder="seu@email.com" value={loginEmail} onChange={(e: any) => setLoginEmail(e.target.value)} isLoading={isLoading} required />
                        <InputField label="Senha" id={`${formId}-login-password`} type="password" placeholder="••••••" value={loginPassword} onChange={(e: any) => setLoginPassword(e.target.value)} isLoading={isLoading} required />

                        <button
                            type="submit"
                            disabled={isLoading}
                            style={{ marginTop: "8px", width: "100%", borderRadius: "8px", backgroundColor: "#f59e0b", padding: "14px", fontWeight: "bold", fontSize: "1rem", color: "#18181b", border: "none", cursor: isLoading ? "not-allowed" : "pointer", opacity: isLoading ? 0.7 : 1 }}
                        >
                            {isLoading ? "Autenticando..." : "Entrar e Finalizar Pedido"}
                        </button>
                    </form>
                ) : (
                    <form onSubmit={handleRegister} style={{ display: "flex", flexDirection: "column", gap: "20px" }}>
                        <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
                            <InputField label="Nome Completo" id={`${formId}-reg-name`} type="text" placeholder="Seu nome" value={regName} onChange={(e: any) => setRegName(e.target.value)} isLoading={isLoading} required />
                            <InputField
                                label="Telefone / WhatsApp"
                                id={`${formId}-reg-phone`}
                                type="tel"
                                inputMode="numeric"
                                placeholder="Somente números (ex: 11999999999)"
                                value={regPhone}
                                onChange={(e: any) => setRegPhone(e.target.value.replace(/\D/g, ''))}
                                isLoading={isLoading}
                            />
                            <InputField label="E-mail" id={`${formId}-reg-email`} type="email" placeholder="seu@email.com" value={regEmail} onChange={(e: any) => setRegEmail(e.target.value)} isLoading={isLoading} required />
                            <InputField label="Senha" id={`${formId}-reg-pass`} type="password" placeholder="Mínimo 6 caracteres" value={regPassword} onChange={(e: any) => setRegPassword(e.target.value)} isLoading={isLoading} required />
                        </div>

                        {/* Divisor Visual de Seção */}
                        <div style={{ position: "relative", marginTop: "8px", marginBottom: "8px" }}>
                            <div style={{ position: "absolute", inset: 0, display: "flex", alignItems: "center" }} aria-hidden="true">
                                <div style={{ width: "100%", borderTop: "1px solid #27272a" }}></div>
                            </div>
                            <div style={{ position: "relative", display: "flex", justifyItems: "center", justifyContent: "center" }}>
                                <span style={{ backgroundColor: "#18181b", padding: "0 12px", fontSize: "0.875rem", fontWeight: 600, color: "#f59e0b" }}>Endereço de Entrega</span>
                            </div>
                        </div>

                        <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
                            <InputField
                                label="CEP"
                                id={`${formId}-reg-cep`}
                                type="text"
                                inputMode="numeric"
                                placeholder="00000000"
                                value={regZipCode}
                                onChange={(e: any) => setRegZipCode(e.target.value.replace(/\D/g, ''))}
                                isLoading={isLoading}
                                required
                            />

                            <div style={{ display: "flex", gap: "16px", flexDirection: isMobile ? "column" : "row" }}>
                                <div style={{ flex: isMobile ? "none" : 3 }}>
                                    <InputField label="Rua / Avenida" id={`${formId}-reg-street`} type="text" placeholder="Nome da rua" value={regStreet} onChange={(e: any) => setRegStreet(e.target.value)} isLoading={isLoading} required />
                                </div>
                                <div style={{ flex: isMobile ? "none" : 1 }}>
                                    <InputField label="Número" id={`${formId}-reg-number`} type="text" placeholder="123" value={regNumber} onChange={(e: any) => setRegNumber(e.target.value)} isLoading={isLoading} required />
                                </div>
                            </div>

                            <InputField label="Complemento / Bairro" id={`${formId}-reg-comp`} type="text" placeholder="Apto 42, Bloco B" value={regSupplement} onChange={(e: any) => setRegSupplement(e.target.value)} isLoading={isLoading} />
                        </div>

                        <button
                            type="submit"
                            disabled={isLoading}
                            style={{ marginTop: "8px", width: "100%", borderRadius: "8px", backgroundColor: "#f59e0b", padding: "14px", fontWeight: "bold", fontSize: "1rem", color: "#18181b", border: "none", cursor: isLoading ? "not-allowed" : "pointer", opacity: isLoading ? 0.7 : 1 }}
                        >
                            {isLoading ? "Cadastrando..." : "Cadastrar e Enviar Pedido"}
                        </button>
                    </form>
                )}
            </div>
        </div>
    );
}