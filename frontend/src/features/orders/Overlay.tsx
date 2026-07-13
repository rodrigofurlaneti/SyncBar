import type { ReactNode } from "react";
import { Modal } from "../../ui/Modal";

interface Props {
  title: string;
  onClose: () => void;
  children: ReactNode;
  wide?: boolean;
}

/**
 * Mantido por compatibilidade com as telas existentes.
 * Agora delega ao Modal acessível (role="dialog", trap de foco, Esc,
 * retorno de foco, scroll lock). `wide` = painel lateral (drawer).
 */
export function Overlay({ title, onClose, children, wide = false }: Props) {
  return (
    <Modal title={title} onClose={onClose} variant={wide ? "drawer" : "center"}>
      {children}
    </Modal>
  );
}
