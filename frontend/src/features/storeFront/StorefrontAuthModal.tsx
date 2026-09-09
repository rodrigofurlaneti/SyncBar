import React, { useState, useId, useEffect } from "react";
import Swal from "sweetalert2";
import { loginCustomerAppUser, registerCustomerAppUser, registerCustomerAddress } from "./storefrontApi";

type StorefrontAuthModalProps = {
    isOpen: boolean;
    onClose: () => void;
    branchId: number;
    onAuthenticated: (customerData: { name: string; phone?: string; customerId?: number }) => void;
};

interface InputFieldProps extends React.InputHTMLAttributes<HTMLInputElement> {
    label: string;
    id: string;
    isLoading?: boolean;
    showFetchingLabel?: boolean;
}

const InputField = ({ label, id, isLoading, showFetchingLabel, ...props }: InputFieldProps) => (
    <div className="input-group">
        <label htmlFor={id} className="input-label">
            {label} {showFetchingLabel && isLoading && <span style={{ fontSize: "0.75rem", color: "#f59e0b", marginLeft: "4px" }}>(Buscando...)</span>}
        </label>
        <input
            id={id}
            disabled={isLoading}
            className="input-field"
            aria-disabled={isLoading}
            {...props}
        />
    </div>
);

function PasswordField({ label, id, isLoading, ...props }: InputFieldProps) {
    const [visible, setVisible] = useState(false);
    return (
        <div className="input-group">
            <label htmlFor={id} className="input-label">{label}</label>
            <div className="auth-password-field">
                <input {...props} id={id} className="input-field" type={visible ? "text" : "password"} disabled={isLoading} />
                <button type="button" className="auth-password-toggle" disabled={isLoading}
                    aria-label={`${visible ? "Ocultar" : "Mostrar"} ${label.toLowerCase()}`}
                    aria-pressed={visible} aria-controls={id} onClick={() => setVisible(value => !value)}>
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                        <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z" />
                        <circle cx="12" cy="12" r="3" />
                        {visible && <path d="m3 3 18 18" />}
                    </svg>
                </button>
            </div>
        </div>
    );
}

const styles = `
  .auth-overlay { position: fixed; inset: 0; z-index: 10000; display: flex; align-items: center; justify-content: center; padding: 1rem; background-color: rgba(0, 0, 0, 0.8); backdrop-filter: blur(0.25rem); animation: fadeIn 0.2s ease-out; font-family: system-ui, -apple-system, sans-serif; }
  .auth-modal { position: relative; width: 100%; max-width: 27.5rem; max-height: 90vh; overflow-y: auto; display: flex; flex-direction: column; gap: 1.5rem; border-radius: 1rem; border: 0.0625rem solid #27272a; background-color: #18181b; padding: 1.5rem; box-shadow: 0 1.5625rem 3.125rem -0.75rem rgba(0, 0, 0, 0.7); animation: slideUp 0.3s cubic-bezier(0.16, 1, 0.3, 1); }
  .auth-header { display: flex; align-items: center; justify-content: space-between; }
  .auth-title { margin: 0; font-size: 1.125rem; font-weight: 600; color: #f4f4f5; }
  .btn-close { background: none; border: none; padding: 0.25rem; color: #a1a1aa; cursor: pointer; border-radius: 0.375rem; display: flex; align-items: center; justify-content: center; transition: all 0.2s ease; }
  .btn-close:hover:not(:disabled) { background-color: #27272a; color: #fff; }
  .btn-close:focus-visible { outline: 0.125rem solid #f59e0b; outline-offset: 0.125rem; }
  .tab-list { display: flex; gap: 0.25rem; border-radius: 0.5rem; background-color: #09090b; padding: 0.25rem; }
  .tab-btn { flex: 1; padding: 0.625rem; border-radius: 0.375rem; border: none; font-weight: 600; font-size: 0.875rem; cursor: pointer; transition: all 0.2s ease; }
  .tab-btn[aria-selected="true"] { background-color: #f59e0b; color: #18181b; }
  .tab-btn[aria-selected="false"] { background-color: transparent; color: #a1a1aa; }
  .tab-btn[aria-selected="false"]:hover:not(:disabled) { color: #fff; background-color: #27272a; }
  .tab-btn:disabled { cursor: not-allowed; opacity: 0.6; }
  .tab-btn:focus-visible { outline: 0.125rem solid #f59e0b; outline-offset: -0.125rem; }
  .form-container { display: flex; flex-direction: column; gap: 1.25rem; }
  .input-group { display: flex; flex-direction: column; gap: 0.375rem; width: 100%; }
  .input-label { font-size: 0.875rem; font-weight: 500; color: #a1a1aa; }
  .input-field { width: 100%; border-radius: 0.5rem; border: 0.0625rem solid #3f3f46; background-color: #09090b; padding: 0.625rem 1rem; color: #f4f4f5; box-sizing: border-box; outline: none; font-size: 0.95rem; transition: border-color 0.2s ease; }
  .input-field:focus:not(:disabled) { border-color: #f59e0b; }
  .input-field:disabled { opacity: 0.5; cursor: not-allowed; }
  .auth-password-field { position: relative; }
  .auth-password-field .input-field { padding-right: 3rem; }
  .auth-password-toggle { position: absolute; right: 0.25rem; top: 0; bottom: 0; width: 2.5rem; display: flex; align-items: center; justify-content: center; background: transparent; border: 0; color: #a1a1aa; cursor: pointer; }
  .auth-password-toggle:focus-visible { outline: 2px solid #f59e0b; border-radius: 0.375rem; }
  .auth-password-error { color: #f87171; font-size: 0.875rem; margin: 0; }
  
  /* ESTILOS PARA CAMPOS BLOQUEADOS (READONLY) */
  .input-field:read-only { background-color: #27272a; color: #a1a1aa; border-color: #27272a; cursor: not-allowed; opacity: 0.8; }
  .input-field:read-only:focus { border-color: #27272a; outline: none; }

  .btn-submit { margin-top: 0.5rem; width: 100%; border-radius: 0.5rem; background-color: #f59e0b; padding: 0.875rem; font-weight: bold; font-size: 1rem; color: #18181b; border: none; cursor: pointer; transition: all 0.2s ease; display: flex; justify-content: center; align-items: center; gap: 0.5rem; }
  .btn-submit:hover:not(:disabled) { background-color: #d97706; }
  .btn-submit:disabled { cursor: not-allowed; opacity: 0.7; }
  .btn-submit:focus-visible { outline: 0.125rem solid #fff; outline-offset: 0.125rem; }
  .address-divider { position: relative; margin: 0.5rem 0; text-align: center; }
  .address-divider::before { content: ""; position: absolute; top: 50%; left: 0; right: 0; border-top: 0.0625rem solid #27272a; z-index: 1; }
  .address-divider span { position: relative; background-color: #18181b; padding: 0 0.75rem; font-size: 0.875rem; font-weight: 600; color: #f59e0b; z-index: 2; }
  .spinner-small { width: 1.25rem; height: 1.25rem; border: 0.125rem solid rgba(24, 24, 27, 0.3); border-left-color: #18181b; border-radius: 50%; animation: spin 1s linear infinite; }
  @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
  @keyframes slideUp { from { opacity: 0; transform: translateY(1rem) scale(0.95); } to { opacity: 1; transform: translateY(0) scale(1); } }
  @keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }
  @media (min-width: 40.0625rem) { .auth-modal { padding: 2rem; } .flex-row-desktop { display: flex; gap: 1rem; flex-direction: row; } .flex-3 { flex: 3; } .flex-1 { flex: 1; } }
  @media (max-width: 40rem) {
    .flex-row-desktop { display: flex; flex-direction: column; gap: 1.25rem; }
    .auth-modal { max-height: 90dvh; box-sizing: border-box; }
    .auth-modal .input-field { font-size: 16px; }
  }
`;

export function StorefrontAuthModal({
    isOpen,
    onClose,
    branchId,
    onAuthenticated,
}: StorefrontAuthModalProps) {
    const [mode, setMode] = useState<"login" | "register">("login");
    const [isLoading, setIsLoading] = useState(false);
    const [isFetchingCep, setIsFetchingCep] = useState(false);

    const formId = useId();

    const [loginEmail, setLoginEmail] = useState("");
    const [loginPassword, setLoginPassword] = useState("");

    const [regName, setRegName] = useState("");
    const [regPhone, setRegPhone] = useState("");
    const [regEmail, setRegEmail] = useState("");
    const [regPassword, setRegPassword] = useState("");
    const [regConfirmPassword, setRegConfirmPassword] = useState("");
    const [passwordValidationAttempted, setPasswordValidationAttempted] = useState(false);
    const passwordError = regPassword.length < 6 || regConfirmPassword.length < 6
        ? "As senhas devem ter no mínimo 6 caracteres."
        : regPassword !== regConfirmPassword ? "As senhas não coincidem." : "";
    const showPasswordError = (passwordValidationAttempted || regConfirmPassword.length > 0) && !!passwordError;
    const [regCpf, setRegCpf] = useState("");
    const [regZipCode, setRegZipCode] = useState("");
    const [regStreet, setRegStreet] = useState("");
    const [regNumber, setRegNumber] = useState("");
    const [regSupplement, setRegSupplement] = useState("");
    const [regNeighborhood, setRegNeighborhood] = useState(""); // Novo estado para Bairro/Cidade

    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === "Escape" && isOpen && !isLoading) {
                onClose();
            }
        };

        if (isOpen) {
            window.addEventListener("keydown", handleKeyDown);
            document.body.style.overflow = "hidden";
        }

        return () => {
            window.removeEventListener("keydown", handleKeyDown);
            document.body.style.overflow = "unset";
        };
    }, [isOpen, isLoading, onClose]);

    if (!isOpen) return null;

    // Busca de CEP Automática (ViaCEP)
    const handleCepChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
        const cep = e.target.value.replace(/\D/g, '').slice(0, 8);
        setRegZipCode(cep);

        if (cep.length === 8) {
            setIsFetchingCep(true);
            try {
                const response = await fetch(`https://viacep.com.br/ws/${cep}/json/`);
                const data = await response.json();

                if (!data.erro) {
                    setRegStreet(data.logradouro || "");
                    setRegNeighborhood(data.bairro ? `${data.bairro}, ${data.localidade} - ${data.uf}` : "");
                    // Foca no número para dar agilidade
                    document.getElementById(`${formId}-reg-number`)?.focus();
                } else {
                    Swal.fire({ toast: true, position: 'top-end', icon: 'error', title: 'CEP não encontrado', showConfirmButton: false, timer: 2000, background: '#18181b', color: '#fff' });
                    setRegStreet("");
                    setRegNeighborhood("");
                }
            } catch (error) {
                console.error("Erro na busca do CEP:", error);
            } finally {
                setIsFetchingCep(false);
            }
        }
    };

    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!loginEmail || !loginPassword) {
            Swal.fire({ title: "Atenção", text: "Preencha o e-mail e a senha.", icon: "warning", background: '#18181b', color: '#fff' });
            return;
        }

        setIsLoading(true);
        try {
            const payload = {
                email: loginEmail,
                password: loginPassword,
                companyId: 1,
                branchId: branchId
            };

            const res = await loginCustomerAppUser(payload);

            onAuthenticated({
                name: res.userName,
                customerId: res.customerId
            });
            onClose();
        } catch (err: any) {
            Swal.fire({ title: "Erro", text: err.message || "E-mail ou senha incorretos.", icon: "error", background: '#18181b', color: '#fff' });
        } finally {
            setIsLoading(false);
        }
    };

    const handleRegister = async (e: React.FormEvent) => {
        e.preventDefault();
        setPasswordValidationAttempted(true);
        if (passwordError) return;
        if (!regName || !regEmail || !regPassword || !regStreet || !regNumber || !regZipCode || !regCpf || !regNeighborhood) {
            Swal.fire({ title: "Atenção", text: "Preencha os campos obrigatórios e digite um CEP válido.", icon: "warning", background: '#18181b', color: '#fff' });
            return;
        }

        setIsLoading(true);
        try {
            const userPayload = {
                branchId: branchId,
                userName: regName,
                email: regEmail,
                password: regPassword,
                phone: regPhone || null,
                cpf: regCpf,
            };

            const userResult = await registerCustomerAppUser(userPayload);

            const newCustomerId = userResult.id;
            await loginCustomerAppUser({ email: regEmail, password: regPassword,
                companyId: userResult.companyId, branchId });

            // Concatenando Complemento + Bairro/Cidade para o backend não perder nenhum dado
            const fullSupplementInfo = regSupplement
                ? `${regSupplement} - ${regNeighborhood}`
                : regNeighborhood;

            const addressPayload = {
                companyId: userResult.companyId,
                branchId: branchId,
                customerId: newCustomerId,
                street: regStreet,
                number: regNumber,
                supplement: fullSupplementInfo,
                zipCode: regZipCode,
            };

            await registerCustomerAddress(addressPayload);

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
            Swal.fire({ title: "Erro", text: err.message || "Não foi possível realizar o cadastro.", icon: "error", background: '#18181b', color: '#fff' });
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div
            data-testid="storefront-auth-modal"
            className="auth-overlay"
            role="dialog"
            aria-modal="true"
            aria-labelledby={`${formId}-modal-title`}
            onClick={isLoading ? undefined : onClose}
        >
            <style>{styles}</style>

            <article
                className="auth-modal"
                onClick={(e) => e.stopPropagation()}
            >
                <header className="auth-header">
                    <h3 id={`${formId}-modal-title`} className="auth-title">
                        {mode === "login" ? <><span aria-hidden="true">🔐</span> Identificação</> : <><span aria-hidden="true">📝</span> Novo Cadastro</>}
                    </h3>
                    <button
                        onClick={onClose}
                        aria-label="Fechar janela de autenticação"
                        disabled={isLoading}
                        className="btn-close"
                    >
                        <svg width="24" height="24" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                    </button>
                </header>

                <nav className="tab-list" role="tablist" aria-label="Selecione o tipo de autenticação">
                    <button
                        type="button"
                        role="tab"
                        aria-selected={mode === "login"}
                        aria-controls={`${formId}-panel-login`}
                        onClick={() => setMode("login")}
                        disabled={isLoading}
                        className="tab-btn"
                    >
                        Já tenho cadastro
                    </button>
                    <button
                        type="button"
                        role="tab"
                        aria-selected={mode === "register"}
                        aria-controls={`${formId}-panel-register`}
                        onClick={() => setMode("register")}
                        disabled={isLoading}
                        className="tab-btn"
                    >
                        Novo Cliente
                    </button>
                </nav>

                <div role="tabpanel" id={`${formId}-panel-${mode}`}>
                    {mode === "login" ? (
                        <form onSubmit={handleLogin} className="form-container" noValidate>
                            <InputField label="E-mail" id={`${formId}-login-email`} type="email" placeholder="seu@email.com" value={loginEmail} onChange={(e: any) => setLoginEmail(e.target.value)} isLoading={isLoading} required />
                            <InputField label="Senha" id={`${formId}-login-password`} type="password" placeholder="••••••" value={loginPassword} onChange={(e: any) => setLoginPassword(e.target.value)} isLoading={isLoading} required />

                            <button type="submit" disabled={isLoading} className="btn-submit" aria-live="polite">
                                {isLoading ? <><div className="spinner-small" /> Autenticando...</> : "Entrar e Finalizar Pedido"}
                            </button>
                        </form>
                    ) : (
                        <form onSubmit={handleRegister} className="form-container" noValidate>
                            <InputField label="Nome Completo" id={`${formId}-reg-name`} type="text" placeholder="Seu nome" value={regName} onChange={(e: any) => setRegName(e.target.value)} isLoading={isLoading} required />

                            <InputField
                                label="CPF"
                                id={`${formId}-reg-cpf`}
                                type="text"
                                inputMode="numeric"
                                placeholder="Somente números (11 dígitos)"
                                value={regCpf}
                                onChange={(e: any) => setRegCpf(e.target.value.replace(/\D/g, '').slice(0, 11))}
                                maxLength={11}
                                isLoading={isLoading}
                                required
                            />

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
                            <PasswordField label="Senha" id={`${formId}-reg-pass`} placeholder="Mínimo 6 caracteres"
                                value={regPassword} onChange={e => setRegPassword(e.target.value)} isLoading={isLoading}
                                minLength={6} autoComplete="new-password" required
                                aria-invalid={showPasswordError} aria-describedby={showPasswordError ? `${formId}-password-error` : undefined} />
                            <PasswordField label="Confirmar Senha" id={`${formId}-reg-confirm-pass`} placeholder="Repita a senha"
                                value={regConfirmPassword} onChange={e => setRegConfirmPassword(e.target.value)} isLoading={isLoading}
                                minLength={6} autoComplete="new-password" required
                                aria-invalid={showPasswordError} aria-describedby={showPasswordError ? `${formId}-password-error` : undefined} />
                            {showPasswordError && <p id={`${formId}-password-error`} className="auth-password-error" role="alert">{passwordError}</p>}

                            <div className="address-divider" aria-hidden="true">
                                <span>Endereço de Entrega</span>
                            </div>

                            <InputField
                                label="CEP"
                                id={`${formId}-reg-cep`}
                                type="text"
                                inputMode="numeric"
                                placeholder="00000000"
                                value={regZipCode}
                                onChange={handleCepChange}
                                maxLength={8}
                                isLoading={isLoading}
                                required
                            />

                            {/* RUA - APENAS LEITURA */}
                            <InputField
                                label="Rua / Avenida"
                                id={`${formId}-reg-street`}
                                type="text"
                                placeholder="Preenchido pelo CEP"
                                value={regStreet}
                                readOnly
                                tabIndex={-1}
                                isLoading={isLoading || isFetchingCep}
                                showFetchingLabel={true}
                                required
                            />

                            <div className="flex-row-desktop">
                                <div className="flex-1">
                                    <InputField
                                        label="Número"
                                        id={`${formId}-reg-number`}
                                        type="text"
                                        placeholder="Ex: 123"
                                        value={regNumber}
                                        onChange={(e: any) => setRegNumber(e.target.value)}
                                        isLoading={isLoading || isFetchingCep}
                                        required
                                    />
                                </div>
                                <div className="flex-3">
                                    {/* COMPLEMENTO - LIVRE PARA DIGITAR */}
                                    <InputField
                                        label="Complemento"
                                        id={`${formId}-reg-comp`}
                                        type="text"
                                        placeholder="Apto, Bloco, etc."
                                        value={regSupplement}
                                        onChange={(e: any) => setRegSupplement(e.target.value)}
                                        isLoading={isLoading || isFetchingCep}
                                    />
                                </div>
                            </div>

                            {/* BAIRRO / CIDADE - APENAS LEITURA */}
                            <InputField
                                label="Bairro / Cidade"
                                id={`${formId}-reg-neighborhood`}
                                type="text"
                                placeholder="Preenchido pelo CEP"
                                value={regNeighborhood}
                                readOnly
                                tabIndex={-1}
                                isLoading={isLoading || isFetchingCep}
                                showFetchingLabel={true}
                            />

                            <button type="submit" disabled={isLoading || isFetchingCep} className="btn-submit" aria-live="polite">
                                {isLoading || isFetchingCep ? <><div className="spinner-small" /> Processando...</> : "Cadastrar e Enviar Pedido"}
                            </button>
                        </form>
                    )}
                </div>
            </article>
        </div>
    );
}
