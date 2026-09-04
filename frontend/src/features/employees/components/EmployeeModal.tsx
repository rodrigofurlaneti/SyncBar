import type { UseMutationResult } from "@tanstack/react-query";
import type { EmployeeResponse, FeatureResponse, JobTitleResponse } from "../../../lib/types";
import type { FormState, AccessFormState } from "../EmployeesPage";
import { Modal } from "../../../ui/Modal";
import { Button } from "../../../ui/Button";
import { Field, TextField } from "../../../ui/Field";
import { Switch } from "../../../ui/Switch";

const CheckIcon = () => (
    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path d="M4 12l5 5L20 6" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
);

const PerfilIcon = () => (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <circle cx="12" cy="8" r="4" stroke="currentColor" strokeWidth="2" />
        <path d="M4 20c0-4 3.6-7 8-7s8 3 8 7" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
);

interface Props {
    editing: EmployeeResponse | "new";
    form: FormState;
    setForm: React.Dispatch<React.SetStateAction<FormState>>;
    access: AccessFormState;
    setAccess: React.Dispatch<React.SetStateAction<AccessFormState>>;
    error: string | null;
    creatingJobTitle: boolean;
    setCreatingJobTitle: (val: boolean) => void;
    newJobTitle: string;
    setNewJobTitle: (val: string) => void;
    jobTitles: JobTitleResponse[];
    jobTitleName: Map<number, string>;
    jobTitleIdNum: number | null;
    features: FeatureResponse[];
    cargoFeatureIds: Set<number>;
    extraFeatureIds: number[];
    toggleSystemAccess: (checked: boolean) => void;
    toggleExtraFeature: (featureId: number) => void;
    jobTitleMutation: UseMutationResult<number, unknown, void, unknown>;
    saveMutation: UseMutationResult<any, unknown, void, unknown>;
    accessFieldsValid: boolean;
    onClose: () => void;
}

export function EmployeeModal({
    editing,
    form,
    setForm,
    access,
    setAccess,
    error,
    creatingJobTitle,
    setCreatingJobTitle,
    newJobTitle,
    setNewJobTitle,
    jobTitles,
    jobTitleName,
    jobTitleIdNum,
    features,
    cargoFeatureIds,
    extraFeatureIds,
    toggleSystemAccess,
    toggleExtraFeature,
    jobTitleMutation,
    saveMutation,
    accessFieldsValid,
    onClose,
}: Props) {
    return (
        <Modal title={editing === "new" ? "Novo funcionário" : "Editar funcionário"} onClose={onClose} wide>
            <TextField
                data-testid="input-emp-name"
                label="Nome"
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                autoFocus
            />

            <div className="ui-row ui-row-wrap" style={{ alignItems: "end" }}>
                <div style={{ flex: 1, minWidth: 160 }}>
                    <TextField
                        data-testid="input-emp-cpf"
                        label="CPF (11 dígitos)"
                        inputMode="numeric"
                        maxLength={11}
                        disabled={editing !== "new"}
                        value={form.cpf}
                        onChange={(e) => setForm({ ...form, cpf: e.target.value.replace(/\D/g, "") })}
                    />
                </div>
                <div style={{ flex: 1, minWidth: 220 }}>
                    <Field
                        label={
                            <span className="ui-row" style={{ justifyContent: "space-between", width: "100%" }}>
                                Cargo
                                {!creatingJobTitle && (
                                    <button
                                        type="button"
                                        data-testid="btn-toggle-new-jobtitle"
                                        onClick={() => setCreatingJobTitle(true)}
                                        style={{ background: "transparent", border: "none", color: "var(--amber)", cursor: "pointer", fontSize: "0.8rem", padding: 0 }}
                                    >
                                        + novo cargo
                                    </button>
                                )}
                            </span>
                        }
                    >
                        {(a11y) =>
                            creatingJobTitle ? (
                                <div className="ui-row" style={{ gap: 6 }}>
                                    <input
                                        {...a11y}
                                        data-testid="input-new-jobtitle"
                                        type="text"
                                        autoFocus
                                        placeholder="Nome do cargo"
                                        value={newJobTitle}
                                        onChange={(e) => setNewJobTitle(e.target.value)}
                                        style={{ flex: 1, minWidth: 0 }}
                                    />
                                    <Button
                                        size="sm"
                                        data-testid="btn-submit-new-jobtitle"
                                        loading={jobTitleMutation.isPending}
                                        disabled={newJobTitle.trim() === ""}
                                        onClick={() => jobTitleMutation.mutate()}
                                    >
                                        Criar
                                    </Button>
                                    <Button
                                        size="sm"
                                        iconOnly
                                        aria-label="Cancelar criação de cargo"
                                        onClick={() => { setCreatingJobTitle(false); setNewJobTitle(""); }}
                                    >
                                        ✕
                                    </Button>
                                </div>
                            ) : (
                                <select
                                    {...a11y}
                                    data-testid="select-emp-jobtitle"
                                    value={form.jobTitleId}
                                    onChange={(e) => setForm({ ...form, jobTitleId: e.target.value })}
                                >
                                    <option value="">Selecione o cargo…</option>
                                    {jobTitles.map((j) => (
                                        <option key={j.id} value={j.id}>{j.name}</option>
                                    ))}
                                </select>
                            )
                        }
                    </Field>
                </div>
            </div>

            <div className="ui-row ui-row-wrap">
                <div style={{ flex: 1, minWidth: 160 }}>
                    <TextField
                        data-testid="input-emp-email"
                        label="E-mail"
                        value={form.email}
                        onChange={(e) => setForm({ ...form, email: e.target.value })}
                    />
                </div>
                <div style={{ flex: 1, minWidth: 160 }}>
                    <TextField
                        data-testid="input-emp-phone"
                        label="Telefone"
                        value={form.phone}
                        onChange={(e) => setForm({ ...form, phone: e.target.value })}
                    />
                </div>
            </div>

            <TextField
                data-testid="input-emp-salary"
                label="Salário (R$, opcional)"
                inputMode="decimal"
                value={form.salary}
                onChange={(e) => setForm({ ...form, salary: e.target.value })}
            />

            {editing === "new" && (
                <div style={{ display: "grid", gap: 14, marginTop: 4 }}>
                    <div className="emp-divider-label">
                        <span className="emp-divider-line" />
                        <span>Acesso ao sistema</span>
                        <span className="emp-divider-line" />
                    </div>

                    <div className="emp-toggle-row">
                        <div className="emp-toggle-copy">
                            <div className="emp-toggle-title">Este colaborador usa o sistema</div>
                            <div className="emp-toggle-hint">
                                Ative só para quem vai operar o PDV — copa, limpeza e segurança normalmente ficam desligados.
                            </div>
                        </div>
                        <Switch
                            checked={access.hasSystemAccess}
                            onChange={toggleSystemAccess}
                            label="Este colaborador usa o sistema"
                        />
                    </div>

                    {access.hasSystemAccess && (
                        <div style={{ display: "grid", gap: 14 }}>
                            <div className="ui-row ui-row-wrap">
                                <div style={{ flex: 1, minWidth: 160 }}>
                                    <TextField
                                        data-testid="input-access-username"
                                        label="Usuário"
                                        value={access.userName}
                                        onChange={(e) => setAccess({ ...access, userName: e.target.value })}
                                    />
                                </div>
                                <div style={{ flex: 1, minWidth: 160 }}>
                                    <TextField
                                        data-testid="input-access-email"
                                        label="E-mail de login"
                                        value={access.userEmail}
                                        onChange={(e) => setAccess({ ...access, userEmail: e.target.value })}
                                    />
                                </div>
                            </div>
                            <TextField
                                data-testid="input-access-password"
                                label="Senha (mín. 8 caracteres)"
                                type="password"
                                value={access.password}
                                onChange={(e) => setAccess({ ...access, password: e.target.value })}
                            />

                            {jobTitleIdNum !== null && (
                                <div className="emp-perfil-auto-card">
                                    <div className="emp-perfil-auto-icon"><PerfilIcon /></div>
                                    <div className="emp-perfil-auto-copy">
                                        <b>Perfil: {jobTitleName.get(jobTitleIdNum) ?? "—"}</b>
                                        <span>Criado automaticamente a partir do cargo — sem passo extra.</span>
                                    </div>
                                </div>
                            )}

                            {cargoFeatureIds.size > 0 && (
                                <div>
                                    <div className="emp-access-group-label">O cargo já libera:</div>
                                    <div className="emp-chips-wrap">
                                        {features
                                            .filter((f) => cargoFeatureIds.has(f.id))
                                            .map((f) => (
                                                <span key={f.id} className="emp-access-chip is-locked">
                                                    <CheckIcon /> {f.name}
                                                </span>
                                            ))}
                                    </div>
                                </div>
                            )}

                            <div>
                                <div className="emp-access-group-label">Acessos extras só para esta pessoa</div>
                                <div className="emp-chips-wrap">
                                    {features
                                        .filter((f) => !cargoFeatureIds.has(f.id))
                                        .map((f) => {
                                            const on = extraFeatureIds.includes(f.id);
                                            return (
                                                <button
                                                    key={f.id}
                                                    type="button"
                                                    className={`emp-access-chip ${on ? "is-on" : "is-off"}`}
                                                    onClick={() => toggleExtraFeature(f.id)}
                                                >
                                                    {on && <CheckIcon />} {f.name}
                                                </button>
                                            );
                                        })}
                                </div>
                            </div>
                        </div>
                    )}
                </div>
            )}

            {error && <p className="error-text">{error}</p>}
            {form.jobTitleId === "" && (
                <p className="field-hint" style={{ margin: 0 }}>
                    Selecione um cargo para habilitar o salvar.
                </p>
            )}

            <Button
                data-testid="btn-submit-employee"
                variant="primary"
                block
                loading={saveMutation.isPending}
                disabled={
                    form.name.trim() === "" ||
                    form.jobTitleId === "" ||
                    (editing === "new" && form.cpf.length !== 11) ||
                    !accessFieldsValid
                }
                onClick={() => saveMutation.mutate()}
            >
                Salvar
            </Button>
        </Modal>
    );
}