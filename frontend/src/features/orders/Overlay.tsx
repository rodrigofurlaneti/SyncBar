import type { ReactNode } from "react";
import { Modal } from "../../ui/Modal";

interface Props {
    title: string;
    onClose: () => void;
    children: ReactNode;
    wide?: boolean;
    "data-testid"?: string; // Adicionado para repassar a tag de testes
}

export function Overlay({
    title,
    onClose,
    children,
    wide = false,
    "data-testid": testId
}: Props) {
    return (
        <Modal
            title={title}
            onClose={onClose}
            variant={wide ? "drawer" : "center"}
            data-testid={testId}
        >
            {children}
        </Modal>
    );
}