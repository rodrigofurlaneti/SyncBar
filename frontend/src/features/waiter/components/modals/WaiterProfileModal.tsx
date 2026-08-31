interface WaiterProfileModalProps {
    userName: string | null;
    branchId: number;
    onClose: () => void;
    onLogout: () => void;
}

export function WaiterProfileModal({ userName, branchId, onClose, onLogout }: WaiterProfileModalProps) {
    return (
        <div
            className="modal-backdrop is-center"
            onMouseDown={(e) => {
                if (e.target === e.currentTarget) onClose();
            }}
            style={{ position: "absolute" }}
        >
            <div className="modal-panel is-center">
                <div className="modal-head">
                    <span className="display" style={{ fontSize: "1.3rem" }}>Perfil</span>
                    <button type="button" className="btn-ghost btn-icon" onClick={onClose}>✕</button>
                </div>
                <div style={{ display: "grid", gap: 4, marginBottom: "12px" }}>
                    <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Usuário</span>
                    <span>{userName ?? "—"}</span>
                </div>
                <div style={{ display: "grid", gap: 4, marginBottom: "20px" }}>
                    <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Filial</span>
                    <span>Filial {branchId}</span>
                </div>
                <button type="button" className="btn-danger btn-block" onClick={onLogout}>
                    Sair
                </button>
            </div>
        </div>
    );
}