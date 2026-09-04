import type { UseMutationResult } from "@tanstack/react-query";
import type { EmployeeResponse, FeatureResponse } from "../../../lib/types";
import { Modal } from "../../../ui/Modal";
import { Button } from "../../../ui/Button";
import { Switch } from "../../../ui/Switch";
// ⚠️ Remova a linha do QueryError daqui

const CheckIcon = () => (
    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path d="M4 12l5 5L20 6" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
);

interface Props {
    employee: EmployeeResponse;
    jobTitleName: Map<number, string>;
    features: FeatureResponse[];
    drawerCargoFeatureIds: Set<number>;
    drawerExtras: number[];
    toggleDrawerExtra: (featureId: number) => void;
    drawerSaveMutation: UseMutationResult<any, unknown, void, unknown>;
    deactivateLoginMutation: UseMutationResult<any, unknown, void, unknown>;
    isDrawerUserFeaturesLoading: boolean;
    error: string | null;
    onClose: () => void;
    onConfirmDeactivate: () => void;
}

export function EmployeeAccessDrawer({
    employee,
    jobTitleName,
    features,
    drawerCargoFeatureIds,
    drawerExtras,
    toggleDrawerExtra,
    drawerSaveMutation,
    deactivateLoginMutation,
    isDrawerUserFeaturesLoading,
    error,
    onClose,
    onConfirmDeactivate,
}: Props) {
    return (
        <Modal
            title={`Acessos — ${employee.name}`}
            onClose={onClose}
            variant="drawer"
            ariaLabel={`Acessos de ${employee.name}`}
        >
            <p className="emp-info-line">
                O acesso efetivo é a soma do que o cargo já dá com o que for ligado abaixo, só para esta pessoa.
            </p>

            {drawerCargoFeatureIds.size > 0 && (
                <div>
                    <div className="emp-access-group-label" style={{ marginBottom: 8 }}>
                        Já incluso pelo cargo "{jobTitleName.get(employee.jobTitleId) ?? ""}"
                    </div>
                    <div className="emp-locked-list">
                        {features
                            .filter((f) => drawerCargoFeatureIds.has(f.id))
                            .map((f) => (
                                <div className="emp-locked-row" key={f.id}>
                                    <CheckIcon /> {f.name}
                                </div>
                            ))}
                    </div>
                </div>
            )}

            <div>
                <div className="emp-access-group-label">Acessos extras desta pessoa</div>
                <div className="emp-toggle-list">
                    {features
                        .filter((f) => !drawerCargoFeatureIds.has(f.id))
                        .map((f) => (
                            <div className="emp-toggle-item" key={f.id}>
                                <span>{f.name}</span>
                                <Switch
                                    checked={drawerExtras.includes(f.id)}
                                    onChange={() => toggleDrawerExtra(f.id)}
                                    label={drawerExtras.includes(f.id) ? `Remover acesso a ${f.name}` : `Conceder acesso a ${f.name}`}
                                />
                            </div>
                        ))}
                </div>
            </div>

            {error && <p className="error-text">{error}</p>}

            <Button
                data-testid="btn-save-drawer-access"
                variant="primary"
                block
                loading={drawerSaveMutation.isPending}
                disabled={isDrawerUserFeaturesLoading}
                onClick={() => drawerSaveMutation.mutate()}
            >
                Salvar acessos
            </Button>

            <div className="emp-danger-row" style={{ borderTop: "1px solid var(--line)", paddingTop: 14, marginTop: 2 }}>
                <div className="emp-danger-copy">
                    <b>Desativar login</b>
                    <span>O funcionário continua na equipe, só perde o acesso ao sistema.</span>
                </div>
                <Button
                    variant="danger"
                    size="sm"
                    loading={deactivateLoginMutation.isPending}
                    onClick={onConfirmDeactivate}
                >
                    Desativar
                </Button>
            </div>
        </Modal>
    );
}