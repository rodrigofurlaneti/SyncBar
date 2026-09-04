import { useEffect, useRef, useState } from "react";
import { Html5Qrcode } from "html5-qrcode";
import Swal from "sweetalert2"; // Adicionado SweetAlert2
import { validateComandaReading, validateTableReading } from "./api";

export type ReadingValidationRequirement = {
    isCameraInputEnabled: boolean;
    isBarcodeEnabled: boolean;
    isQrCodeEnabled: boolean;
};

/** Basta UM dos cenários ligados ser concluído — não é preciso completar todos. */
export const needsReadingValidation = (r: ReadingValidationRequirement): boolean =>
    r.isCameraInputEnabled || r.isBarcodeEnabled || r.isQrCodeEnabled;

type SubmitPayload = { scannedValue?: string; photoBase64?: string };
type SubmitFn = (method: "camera" | "barcode" | "qrcode", payload: SubmitPayload) => Promise<void>;

type GateProps = {
    requirement: ReadingValidationRequirement;
    subtitle: string;
    submit: SubmitFn;
    onValidated: () => void;
    onCancel: () => void;
};

const SCANNER_ELEMENT_ID = "reading-validation-scanner";

const cameraErrorMessage = (err: unknown): string => {
    const name = err instanceof Error ? err.name : "";
    switch (name) {
        case "NotAllowedError":
            return "O navegador bloqueou o acesso à câmera. Libere a permissão de câmera para este site nas configurações do navegador e tente de novo.";
        case "NotFoundError":
        case "OverconstrainedError":
            return "Nenhuma câmera foi encontrada neste dispositivo.";
        case "NotReadableError":
            return "A câmera já está sendo usada por outro aplicativo ou aba. Feche-o e tente de novo.";
        default:
            return "Não foi possível acessar a câmera. Verifique a permissão do navegador.";
    }
};

const safeStopScanner = (ref: { current: Html5Qrcode | null }) => {
    try {
        ref.current?.stop().catch(() => undefined);
    } catch {
        // Scanner já parado/parando — nada a fazer.
    }
};

function ReadingValidationGate({ requirement, subtitle, submit, onValidated, onCancel }: GateProps) {
    const [scanning, setScanning] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const fileInputRef = useRef<HTMLInputElement>(null);
    const scannerRef = useRef<Html5Qrcode | null>(null);
    const hasDecodedRef = useRef(false);

    const wantsScan = requirement.isBarcodeEnabled || requirement.isQrCodeEnabled;
    const scanMethod = requirement.isQrCodeEnabled ? "qrcode" : "barcode";

    useEffect(() => {
        return () => {
            safeStopScanner(scannerRef);
        };
    }, []);

    const doSubmit = async (method: "camera" | "barcode" | "qrcode", payload: SubmitPayload) => {
        setSubmitting(true);
        try {
            await submit(method, payload);
            onValidated();
        } catch {
            Swal.fire("Erro", "Não foi possível confirmar a validação. Tente novamente.", "error");
        } finally {
            setSubmitting(false);
        }
    };

    const handlePhotoSelected = (file: File) => {
        const reader = new FileReader();
        reader.onload = () => {
            const dataUrl = typeof reader.result === "string" ? reader.result : "";
            if (dataUrl) void doSubmit("camera", { photoBase64: dataUrl });
        };
        reader.readAsDataURL(file);
    };

    const startScan = async () => {
        setScanning(true);
        hasDecodedRef.current = false;
        try {
            const scanner = new Html5Qrcode(SCANNER_ELEMENT_ID);
            scannerRef.current = scanner;
            const onDecoded = (decodedText: string) => {
                if (hasDecodedRef.current) return;
                hasDecodedRef.current = true;
                const finishSubmit = () => {
                    setScanning(false);
                    void doSubmit(scanMethod, { scannedValue: decodedText });
                };
                try {
                    void scanner.stop().catch(() => undefined).finally(finishSubmit);
                } catch {
                    finishSubmit();
                }
            };
            const onFrame = () => { /* frame sem leitura — ignora e continua tentando */ };
            try {
                await scanner.start({ facingMode: "environment" }, { fps: 10, qrbox: { width: 240, height: 240 } }, onDecoded, onFrame);
            } catch {
                await scanner.start({ facingMode: "user" }, { fps: 10, qrbox: { width: 240, height: 240 } }, onDecoded, onFrame);
            }
        } catch (err) {
            setScanning(false);
            Swal.fire("Atenção", cameraErrorMessage(err), "warning");
        }
    };

    const stopScan = () => {
        safeStopScanner(scannerRef);
        setScanning(false);
    };

    return (
        <div style={{ display: "grid", gap: 16, textAlign: "center" }} data-testid="reading-validation-gate">
            <div>
                <h3 style={{ margin: "0 0 6px", color: "#fff", fontSize: "1.15rem" }}>Confirme para continuar</h3>
                <p style={{ margin: 0, color: "#a8a8b3", fontSize: "0.9rem" }}>{subtitle}</p>
            </div>

            <div id={SCANNER_ELEMENT_ID} data-testid="scanner-container" style={{ width: "100%", borderRadius: 8, overflow: "hidden", display: scanning ? "block" : "none" }} />

            {scanning ? (
                <button type="button" onClick={stopScan} className="btn-ghost" data-testid="btn-cancel-scan">
                    Cancelar leitura
                </button>
            ) : (
                <div style={{ display: "grid", gap: 10 }}>
                    {requirement.isCameraInputEnabled && (
                        <>
                            <input
                                ref={fileInputRef}
                                type="file"
                                accept="image/*"
                                capture="user"
                                style={{ display: "none" }}
                                data-testid="input-camera-file"
                                onChange={(e) => {
                                    const file = e.target.files?.[0];
                                    if (file) handlePhotoSelected(file);
                                    e.target.value = "";
                                }}
                            />
                            <button
                                type="button"
                                disabled={submitting}
                                onClick={() => fileInputRef.current?.click()}
                                data-testid="btn-take-photo"
                                style={{ padding: 14, borderRadius: 8, border: "1px solid #323238", backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold", cursor: submitting ? "not-allowed" : "pointer" }}
                            >
                                📷 Tirar uma foto
                            </button>
                        </>
                    )}
                    {wantsScan && (
                        <button
                            type="button"
                            disabled={submitting}
                            onClick={() => void startScan()}
                            data-testid="btn-start-scan"
                            style={{ padding: 14, borderRadius: 8, border: "1px solid #323238", backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold", cursor: submitting ? "not-allowed" : "pointer" }}
                        >
                            {requirement.isQrCodeEnabled ? "▦ Escanear QR Code" : "▥ Escanear código de barras"}
                        </button>
                    )}
                    <button type="button" onClick={onCancel} disabled={submitting} className="btn-ghost" data-testid="btn-cancel-validation">
                        Cancelar
                    </button>
                </div>
            )}

            {submitting && <p style={{ color: "#a8a8b3", fontSize: "0.9rem" }} data-testid="msg-confirming">Confirmando…</p>}
        </div>
    );
}

type ComandaProps = {
    token: string;
    comandaCode: string;
    requirement: ReadingValidationRequirement;
    onValidated: () => void;
    onCancel: () => void;
};

export function ComandaReadingValidation({ token, comandaCode, requirement, onValidated, onCancel }: ComandaProps) {
    return (
        <ReadingValidationGate
            requirement={requirement}
            subtitle={`Esta filial exige uma confirmação extra para abrir a comanda ${comandaCode}. Escolha uma das opções abaixo.`}
            submit={(method, payload) => validateComandaReading(token, comandaCode, { method, ...payload })}
            onValidated={onValidated}
            onCancel={onCancel}
        />
    );
}

type TableProps = {
    token: string;
    requirement: ReadingValidationRequirement;
    onValidated: () => void;
    onCancel: () => void;
};

export function TableReadingValidation({ token, requirement, onValidated, onCancel }: TableProps) {
    return (
        <ReadingValidationGate
            requirement={requirement}
            subtitle="Esta filial exige uma confirmação extra antes de fazer pedidos nesta mesa. Escolha uma das opções abaixo."
            submit={(method, payload) => validateTableReading(token, { method, ...payload })}
            onValidated={onValidated}
            onCancel={onCancel}
        />
    );
}

type LinkComandaProps = {
    token: string;
    requirement: ReadingValidationRequirement;
    onLinked: (comandaCode: string) => void;
    onCancel: () => void;
};

const LINK_SCANNER_ELEMENT_ID = "link-comanda-scanner";

export function LinkComandaValidation({ token, requirement, onLinked, onCancel }: LinkComandaProps) {
    const wantsScan = requirement.isBarcodeEnabled || requirement.isQrCodeEnabled;
    const scanMethod = requirement.isQrCodeEnabled ? "qrcode" : "barcode";

    const [manualCode, setManualCode] = useState("");
    const [scanning, setScanning] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const fileInputRef = useRef<HTMLInputElement>(null);
    const scannerRef = useRef<Html5Qrcode | null>(null);
    const hasDecodedRef = useRef(false);

    useEffect(() => {
        return () => {
            safeStopScanner(scannerRef);
        };
    }, []);

    const linkComanda = async (code: string, method: "camera" | "barcode" | "qrcode", payload: SubmitPayload) => {
        setSubmitting(true);
        try {
            await validateComandaReading(token, code, { method, ...payload });
            onLinked(code);
        } catch {
            Swal.fire("Erro", "Não foi possível vincular a comanda. Confira o número/código e tente de novo.", "error");
        } finally {
            setSubmitting(false);
        }
    };

    const handlePhotoSelected = (file: File) => {
        if (!manualCode.trim()) {
            Swal.fire("Atenção", "Informe o número da comanda antes de tirar a foto.", "warning");
            return;
        }
        const reader = new FileReader();
        reader.onload = () => {
            const dataUrl = typeof reader.result === "string" ? reader.result : "";
            if (dataUrl) void linkComanda(manualCode.trim(), "camera", { photoBase64: dataUrl });
        };
        reader.readAsDataURL(file);
    };

    const startScan = async () => {
        setScanning(true);
        hasDecodedRef.current = false;
        try {
            const scanner = new Html5Qrcode(LINK_SCANNER_ELEMENT_ID);
            scannerRef.current = scanner;
            const onDecoded = (decodedText: string) => {
                if (hasDecodedRef.current) return;
                hasDecodedRef.current = true;
                const finishLink = () => {
                    setScanning(false);
                    void linkComanda(decodedText, scanMethod, { scannedValue: decodedText });
                };
                try {
                    void scanner.stop().catch(() => undefined).finally(finishLink);
                } catch {
                    finishLink();
                }
            };
            const onFrame = () => { /* frame sem leitura — ignora e continua tentando */ };
            try {
                await scanner.start({ facingMode: "environment" }, { fps: 10, qrbox: { width: 240, height: 240 } }, onDecoded, onFrame);
            } catch {
                await scanner.start({ facingMode: "user" }, { fps: 10, qrbox: { width: 240, height: 240 } }, onDecoded, onFrame);
            }
        } catch (err) {
            setScanning(false);
            Swal.fire("Atenção", cameraErrorMessage(err), "warning");
        }
    };

    const stopScan = () => {
        safeStopScanner(scannerRef);
        setScanning(false);
    };

    return (
        <div style={{ display: "grid", gap: 16, textAlign: "center" }} data-testid="link-comanda-validation">
            <div>
                <h3 style={{ margin: "0 0 6px", color: "#fff", fontSize: "1.15rem" }}>Vincular à comanda</h3>
                <p style={{ margin: 0, color: "#a8a8b3", fontSize: "0.9rem" }}>
                    {wantsScan
                        ? `Escaneie o ${requirement.isQrCodeEnabled ? "QR Code" : "código de barras"} da comanda para lançar este pedido nela.`
                        : "Informe o número da comanda e tire uma foto dela para lançar este pedido nela."}
                </p>
            </div>

            <div id={LINK_SCANNER_ELEMENT_ID} data-testid="link-scanner-container" style={{ width: "100%", borderRadius: 8, overflow: "hidden", display: scanning ? "block" : "none" }} />

            {scanning ? (
                <button type="button" onClick={stopScan} className="btn-ghost" data-testid="btn-link-cancel-scan">
                    Cancelar leitura
                </button>
            ) : (
                <div style={{ display: "grid", gap: 10 }}>
                    {!wantsScan && requirement.isCameraInputEnabled && (
                        <>
                            <input
                                type="text"
                                value={manualCode}
                                onChange={(e) => setManualCode(e.target.value)}
                                placeholder="Número da comanda (ex: 001)"
                                autoFocus
                                data-testid="input-manual-code"
                                style={{ width: "100%", padding: "14px 16px", borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", fontSize: "1rem", outline: "none", boxSizing: "border-box" }}
                            />
                            <input
                                ref={fileInputRef}
                                type="file"
                                accept="image/*"
                                capture="user"
                                style={{ display: "none" }}
                                data-testid="input-link-camera-file"
                                onChange={(e) => {
                                    const file = e.target.files?.[0];
                                    if (file) handlePhotoSelected(file);
                                    e.target.value = "";
                                }}
                            />
                            <button
                                type="button"
                                disabled={submitting || !manualCode.trim()}
                                onClick={() => fileInputRef.current?.click()}
                                data-testid="btn-link-take-photo"
                                style={{ padding: 14, borderRadius: 8, border: "1px solid #323238", backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold", cursor: submitting || !manualCode.trim() ? "not-allowed" : "pointer" }}
                            >
                                📷 Tirar uma foto
                            </button>
                        </>
                    )}
                    {wantsScan && (
                        <button
                            type="button"
                            disabled={submitting}
                            onClick={() => void startScan()}
                            data-testid="btn-link-start-scan"
                            style={{ padding: 14, borderRadius: 8, border: "1px solid #323238", backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold", cursor: submitting ? "not-allowed" : "pointer" }}
                        >
                            {requirement.isQrCodeEnabled ? "▦ Escanear QR Code da comanda" : "▥ Escanear código de barras da comanda"}
                        </button>
                    )}
                    <button type="button" onClick={onCancel} disabled={submitting} className="btn-ghost" data-testid="btn-link-cancel">
                        Cancelar
                    </button>
                </div>
            )}

            {submitting && <p style={{ color: "#a8a8b3", fontSize: "0.9rem" }} data-testid="msg-link-confirming">Vinculando…</p>}
        </div>
    );
}