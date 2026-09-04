import type { EmployeeResponse } from "../../../lib/types";
import { formatBRL } from "../../../lib/types";
import { Button } from "../../../ui/Button";

const initialsOf = (name: string) =>
    name
        .trim()
        .split(/\s+/)
        .map((p) => p[0])
        .slice(0, 2)
        .join("")
        .toUpperCase();

const KebabIcon = () => (
    <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
        <circle cx="12" cy="5" r="1.8" /><circle cx="12" cy="12" r="1.8" /><circle cx="12" cy="19" r="1.8" />
    </svg>
);

const PerfilIcon = () => (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <circle cx="12" cy="8" r="4" stroke="currentColor" strokeWidth="2" />
        <path d="M4 20c0-4 3.6-7 8-7s8 3 8 7" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
);

interface Props {
    employee: EmployeeResponse;
    jobTitleName: Map<number, string>;
    isOpenKebab: boolean;
    onToggleKebab: () => void;
    onCloseKebab: () => void;
    onEdit: () => void;
    onOpenAccess: () => void;
    onDismiss: () => void;
}

export function EmployeeCard({
    employee,
    jobTitleName,
    isOpenKebab,
    onToggleKebab,
    onCloseKebab,
    onEdit,
    onOpenAccess,
    onDismiss,
}: Props) {
    return (
        <div className={`emp-card ${!employee.hasSystemAccess ? "is-no-login" : ""}`} key={employee.id} data-testid={`employee-card-${employee.id}`}>
            <div className="emp-card-top">
                <div className="emp-avatar">{initialsOf(employee.name)}</div>
                <div className="emp-name-block">
                    <div className="emp-name">{employee.name}</div>
                    <div className="emp-role">{jobTitleName.get(employee.jobTitleId) ?? `Cargo ${employee.jobTitleId}`}</div>
                </div>
                <span className={`emp-status-pill ${employee.hasSystemAccess ? "is-on" : "is-off"}`}>
                    <span className="emp-status-dot" />
                    {employee.hasSystemAccess ? "Acesso ativo" : "Sem login"}
                </span>
            </div>

            <div className="emp-meta">
                CPF {employee.cpf}
                {employee.salary !== null ? ` · ${formatBRL(employee.salary)}` : ""}
            </div>

            {employee.hasSystemAccess && (
                <div className="emp-chip-row">
                    <span className="emp-perfil-chip">
                        <PerfilIcon />
                        {employee.roleName ?? "Perfil"}
                    </span>
                    {employee.extraFeatureCount > 0 && (
                        <span className="emp-extra-chip">
                            +{employee.extraFeatureCount} acesso{employee.extraFeatureCount > 1 ? "s" : ""} extra
                        </span>
                    )}
                </div>
            )}

            <div className="emp-card-footer">
                <Button size="sm" data-testid={`btn-edit-employee-${employee.id}`} onClick={onEdit}>Editar</Button>
                {employee.hasSystemAccess && (
                    <Button
                        size="sm"
                        data-testid={`btn-access-employee-${employee.id}`}
                        onClick={onOpenAccess}
                    >
                        Acessos
                    </Button>
                )}
                <span className="ui-spacer" />
                <Button
                    size="sm"
                    iconOnly
                    data-testid={`btn-kebab-${employee.id}`}
                    aria-label="Mais ações"
                    onClick={onToggleKebab}
                >
                    <KebabIcon />
                </Button>
                {isOpenKebab && (
                    <>
                        <div
                            style={{ position: "fixed", inset: 0, zIndex: 4 }}
                            onClick={onCloseKebab}
                        />
                        <div className="emp-kebab-menu">
                            <button
                                type="button"
                                data-testid={`btn-dismiss-${employee.id}`}
                                className="is-danger"
                                onClick={onDismiss}
                            >
                                Demitir
                            </button>
                        </div>
                    </>
                )}
            </div>
        </div>
    );
}