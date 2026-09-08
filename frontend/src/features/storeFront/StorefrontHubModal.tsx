import React, { useState, useEffect, useCallback, useMemo } from "react";
import Swal from "sweetalert2";
import { useRef } from "react";

type StorefrontHubModalProps = {
    isOpen: boolean;
    onClose: () => void;
    branchId: number;
};

const styles = `
  .hub-modal-overlay {
    position: fixed;
    inset: 0;
    background-color: rgba(0, 0, 0, 0.85);
    z-index: 9999;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 1rem;
    backdrop-filter: blur(0.25rem);
    animation: fadeIn 0.2s ease-out;
  }
  
  .hub-modal-content {
    background-color: #18181b;
    padding: 1.75rem;
    border-radius: 1rem;
    width: 100%;
    max-width: 26.25rem; /* 420px */
    border: 0.0625rem solid #27272a;
    box-shadow: 0 1.5625rem 3.125rem -0.75rem rgba(0, 0, 0, 0.6);
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
    animation: slideUp 0.3s cubic-bezier(0.16, 1, 0.3, 1);
  }

  .hub-modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .hub-modal-title {
    margin: 0;
    color: #fff;
    font-size: 1.15rem;
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .hub-close-btn {
    background: none;
    border: none;
    color: #a1a1aa;
    font-size: 1.5rem;
    cursor: pointer;
    padding: 0.25rem;
    border-radius: 0.375rem;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.2s ease;
  }

  .hub-close-btn:hover {
    color: #fff;
    background-color: #27272a;
  }
  
  .hub-close-btn:focus-visible {
    outline: 0.125rem solid #f59e0b;
    outline-offset: 0.125rem;
  }

  .hub-tab-list {
    display: flex;
    gap: 0.25rem;
    background-color: #09090b;
    padding: 0.25rem;
    border-radius: 0.5rem;
  }

  .hub-tab-btn {
    flex: 1;
    padding: 0.625rem;
    border-radius: 0.375rem;
    border: none;
    font-weight: 600;
    cursor: pointer;
    font-size: 0.85rem;
    transition: all 0.2s ease;
  }

  .hub-tab-btn[aria-selected="true"] {
    background-color: #f59e0b;
    color: #000;
  }

  .hub-tab-btn[aria-selected="false"] {
    background-color: transparent;
    color: #a1a1aa;
  }

  .hub-tab-btn[aria-selected="false"]:hover {
    color: #fff;
    background-color: #27272a;
  }

  .hub-tab-btn:focus-visible {
    outline: 0.125rem solid #f59e0b;
    outline-offset: -0.125rem;
  }

  .hub-input-readonly {
    width: 100%;
    padding: 0.875rem;
    border-radius: 0.5rem;
    border: 0.0625rem solid #3f3f46;
    background-color: #09090b;
    color: #fff;
    font-size: 0.9rem;
    text-align: center;
    outline: none;
    box-sizing: border-box;
    cursor: copy;
    transition: border-color 0.2s ease;
  }

  .hub-input-readonly:focus {
    border-color: #f59e0b;
  }

  .hub-action-btn {
    width: 100%;
    padding: 0.875rem;
    border-radius: 0.5rem;
    border: none;
    background-color: #f59e0b;
    color: #18181b;
    font-weight: 700;
    cursor: pointer;
    font-size: 1rem;
    box-shadow: 0 0.25rem 0.75rem rgba(245, 158, 11, 0.2);
    transition: all 0.2s ease;
  }

  .hub-action-btn:hover {
    background-color: #d97706;
    transform: translateY(-0.0625rem);
  }

  .hub-action-btn:active {
    transform: translateY(0);
  }

  .hub-action-btn:focus-visible {
    outline: 0.125rem solid #fff;
    outline-offset: 0.125rem;
  }

  .qr-container {
    background-color: #fff;
    padding: 0.75rem;
    border-radius: 0.75rem;
    display: inline-flex;
    position: relative;
    min-height: 11.25rem;
    min-width: 11.25rem;
    align-items: center;
    justify-content: center;
  }

  .qr-skeleton {
    position: absolute;
    inset: 0.75rem;
    background: linear-gradient(90deg, #e4e4e7 25%, #f4f4f5 50%, #e4e4e7 75%);
    background-size: 200% 100%;
    animation: shimmer 1.5s infinite;
    border-radius: 0.25rem;
  }

  @keyframes fadeIn {
    from { opacity: 0; }
    to { opacity: 1; }
  }

  @keyframes slideUp {
    from { opacity: 0; transform: translateY(1rem) scale(0.95); }
    to { opacity: 1; transform: translateY(0) scale(1); }
  }

  @keyframes shimmer {
    0% { background-position: 200% 0; }
    100% { background-position: -200% 0; }
  }

  /* Breakpoints */
  @media (min-width: 40.0625rem) { /* 641px - Tablet */
    .hub-modal-content {
      padding: 2rem;
      max-width: 32rem; /* Amplia levemente no tablet */
    }
    .hub-tab-btn {
      font-size: 0.95rem;
    }
  }
`;

export function StorefrontHubModal({
    isOpen,
    onClose,
    branchId,
}: StorefrontHubModalProps) {
    const [activeTab, setActiveTab] = useState<"link" | "qrcode">("link");
    const [isQrLoaded, setIsQrLoaded] = useState(false);
    const linkInputRef = useRef<HTMLInputElement>(null);
    const [manualCopy, setManualCopy] = useState(false);

    // Memoriza a URL para evitar recálculos desnecessários
    const storefrontUrl = useMemo(() => {
        return typeof window !== "undefined" ? `${window.location.origin}/cardapio/${branchId}` : "";
    }, [branchId]);

    // Acessibilidade: Fechar modal ao pressionar a tecla Escape
    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === "Escape" && isOpen) {
                onClose();
            }
        };

        if (isOpen) {
            window.addEventListener("keydown", handleKeyDown);
            // Previne rolagem do fundo enquanto o modal está aberto
            document.body.style.overflow = "hidden";
        }

        return () => {
            window.removeEventListener("keydown", handleKeyDown);
            document.body.style.overflow = "unset";
        };
    }, [isOpen, onClose]);

    const handleCopy = useCallback(async () => {
        setManualCopy(false);
        let copied = false;
        try {
            if (navigator.clipboard?.writeText) {
                await navigator.clipboard.writeText(storefrontUrl);
                copied = true;
            }
        } catch {
            // Permissão negada: tenta a cópia pela seleção do campo abaixo.
        }
        if (!copied) {
            const input = linkInputRef.current;
            const previousFocus = document.activeElement as HTMLElement | null;
            input?.focus({ preventScroll: true });
            input?.select();
            input?.setSelectionRange(0, storefrontUrl.length);
            try {
                // Compatibilidade com acesso HTTP, onde navigator.clipboard não existe.
                copied = !!input && document.execCommand("copy");
            } catch {
                copied = false;
            }
            if (copied) previousFocus?.focus({ preventScroll: true });
        }
        if (copied) {
            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: 'success',
                title: 'Link copiado! Pronto para enviar.',
                showConfirmButton: false,
                timer: 2500,
                background: '#18181b',
                color: '#fff'
            });
        } else {
            setManualCopy(true);
        }
    }, [storefrontUrl]);

    const handleOverlayClick = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
        if (e.target === e.currentTarget) {
            onClose();
        }
    }, [onClose]);

    // Hooks devem ser sempre chamados incondicionalmente, o early return fica após eles.
    if (!isOpen) return null;

    return (
        <div
            data-testid="storefront-hub-modal"
            className="hub-modal-overlay"
            onClick={handleOverlayClick}
            role="dialog"
            aria-modal="true"
            aria-labelledby="modal-title"
        >
            <style>{styles}</style>

            <article className="hub-modal-content">
                <header className="hub-modal-header">
                    <h3 id="modal-title" className="hub-modal-title">
                        <span aria-hidden="true">📱</span> Autoatendimento
                    </h3>
                    <button
                        onClick={onClose}
                        className="hub-close-btn"
                        aria-label="Fechar janela de autoatendimento"
                    >
                        ✕
                    </button>
                </header>

                <nav className="hub-tab-list" role="tablist" aria-label="Opções de compartilhamento">
                    <button
                        role="tab"
                        aria-selected={activeTab === "link"}
                        aria-controls="tabpanel-link"
                        id="tab-link"
                        onClick={() => setActiveTab("link")}
                        className="hub-tab-btn"
                    >
                        <span aria-hidden="true">📲</span> Enviar Link
                    </button>
                    <button
                        role="tab"
                        aria-selected={activeTab === "qrcode"}
                        aria-controls="tabpanel-qrcode"
                        id="tab-qrcode"
                        onClick={() => setActiveTab("qrcode")}
                        className="hub-tab-btn"
                    >
                        <span aria-hidden="true">🖨️</span> QR Code
                    </button>
                </nav>

                <section
                    id={`tabpanel-${activeTab}`}
                    role="tabpanel"
                    aria-labelledby={`tab-${activeTab}`}
                >
                    {activeTab === "link" ? (
                        <div style={{ display: "flex", flexDirection: "column", gap: "1rem", textAlign: "center" }}>
                            <p style={{ margin: 0, color: "#a1a1aa", fontSize: "0.9rem", lineHeight: "1.4" }}>
                                Copie o link abaixo e mande para o cliente fazer o pedido direto do celular dele:
                            </p>
                            <input
                                ref={linkInputRef}
                                type="text"
                                readOnly
                                value={storefrontUrl}
                                onFocus={(e) => e.target.select()}
                                className="hub-input-readonly"
                                aria-label="URL do cardápio"
                            />
                            <button
                                onClick={handleCopy}
                                className="hub-action-btn"
                            >
                                <span aria-hidden="true">📋</span> Copiar Link do Cardápio
                            </button>
                            {manualCopy && <p role="status" style={{ margin: 0, color: "#fbbf24", fontSize: "0.9rem" }}>
                                O navegador bloqueou a cópia automática. O link está selecionado: pressione Ctrl+C (ou ⌘C) ou toque e segure para copiar.
                            </p>}
                        </div>
                    ) : (
                        <div style={{ display: "flex", flexDirection: "column", gap: "1rem", alignItems: "center", textAlign: "center" }}>
                            <p style={{ margin: 0, color: "#a1a1aa", fontSize: "0.9rem", lineHeight: "1.4" }}>
                                Aponte a câmera para o QR Code abaixo para acessar o cardápio:
                            </p>
                            <div className="qr-container">
                                {!isQrLoaded && <div className="qr-skeleton" aria-hidden="true"></div>}
                                <img
                                    src={`https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=${encodeURIComponent(storefrontUrl)}`}
                                    alt="Código QR para acessar o cardápio digital"
                                    width={180}
                                    height={180}
                                    onLoad={() => setIsQrLoaded(true)}
                                    style={{ display: isQrLoaded ? "block" : "none" }}
                                />
                            </div>
                            <input
                                readOnly
                                value={storefrontUrl}
                                onFocus={(e) => e.target.select()}
                                className="hub-input-readonly"
                                style={{ fontSize: "0.8rem", color: "#71717a", padding: "0.75rem" }}
                                aria-label="URL do cardápio"
                            />
                        </div>
                    )}
                </section>
            </article>
        </div>
    );
}
