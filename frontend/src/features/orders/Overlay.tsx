import type { ReactNode } from "react";
import { Modal } from "../../ui/Modal";

interface Props {
  title: string;
  onClose: () => void;
  children: ReactNode;
  wide?: boolean;
}

export function Overlay({ title, onClose, children, wide = false }: Props) {
  return (
    <Modal title={title} onClose={onClose} variant={wide ? "drawer" : "center"}>
      {children}
    </Modal>
  );
}
