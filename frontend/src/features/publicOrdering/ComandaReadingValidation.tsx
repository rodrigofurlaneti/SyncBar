import { useEffect, useRef, useState } from "react";
import { Html5Qrcode } from "html5-qrcode";
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

/**
 * `Html5Qrcode.stop()` lança de forma SÍNCRONA (não retorna uma Promise rejeitada — o
 * throw acontece antes de qualquer Promise existir) quando chamado num scanner que já
 * não está rodando ("Cannot stop, scanner is not running or paused."). Isso acontecia
 * dentro do cleanup do useEffect de desmontagem: depois que `onDecoded` já chamava
 * `scanner.stop()` com sucesso (fluxo normal de leitura), o componente desmontava logo
 * em seguida (ex.: `LinkComandaValidation` some da árvore assim que o `onLinked` do pai
 * muda de tela) e o cleanup chamava `stop()` DE NOVO no mesmo scanner já parado — um
 * `.catch()` encadeado não protege contra um throw síncrono, então essa segunda chamada
 * escapava direto pro React durante a fase de commit (efeito de desmontagem), o que o
 * Error Boundary trata como um crash real e derruba a tela ("Algo deu errado"), mesmo
 * com o pedido já tendo sido concluído com sucesso. Envolve toda chamada a `stop()`
 * (cleanup, botão "Cancelar leitura" e o próprio `onDecoded`) para nunca deixar esse
 * throw síncrono escapar.
 */
const safeStopScanner = (ref: { current: Html5Qrcode | null }) => {
    try {
        ref.current?.stop().catch(() => undefined);
    } catch {
        // Scanner já parado/parando — nada a fazer.
    }
};

/**
 * Trava de segurança comum às validações de leitura (comanda e mesa): exige que o
 * cliente complete pelo menos um dos cenários ligados na filial (câmera, código de
 * barras ou QR Code). "Câmera" = tira uma foto de comprovação (sem OCR, é só registro).
 * "Código de barras" e "QR Code" = escaneia com a câmera do celular (mesmo scanner,
 * formatos diferentes). Não é exportado diretamente — use `ComandaReadingValidation`
 * ou `TableReadingValidation` abaixo, que só diferem em pra onde a comprovação vai.
 */
function ReadingValidationGate({ requirement, subtitle, submit, onValidated, onCancel }: GateProps) {
    const [scanning, setScanning] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const fileInputRef = useRef<HTMLInputElement>(null);
    const scannerRef = useRef<Html5Qrcode | null>(null);
    // O scanner chama o callback de sucesso de novo a cada frame em que o código ainda
    // estiver visível, mesmo depois do primeiro acerto — stop() é assíncrono, então o
    // código físico continua "sendo lido" por alguns frames enquanto a câmera fecha.
    // Sem essa trava, um único escaneamento podia disparar doSubmit (e o pedido) mais
    // de uma vez.
    const hasDecodedRef = useRef(false);

    const wantsScan = requirement.isBarcodeEnabled || requirement.isQrCodeEnabled;
    // Se os dois estiverem ligados, um único escaneamento satisfaz qualquer um dos dois —
    // o método enviado ao backend é só um rótulo de auditoria, não muda a validação em si.
    const scanMethod = requirement.isQrCodeEnabled ? "qrcode" : "barcode";

    useEffect(() => {
        return () => {
            safeStopScanner(scannerRef);
        };
    }, []);

    const doSubmit = async (method: "camera" | "barcode" | "qrcode", payload: SubmitPayload) => {
        setSubmitting(true);
        setError(null);
        try {
            await submit(method, payload);
            onValidated();
        } catch {
            setError("Não foi possível confirmar a validação. Tente novamente.");
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
        setError(null);
        setScanning(true);
        hasDecodedRef.current = false;
        try {
            // `new Html5Qrcode(id)` faz `document.getElementById(id)` e lança se não achar
            // — por isso também fica dentro do try/catch agora: mesmo que o container não
            // esteja montado por algum motivo inesperado, o erro aparece pro usuário em vez
            // de sumir silenciosamente (era o que acontecia antes dessa correção).
            const scanner = new Html5Qrcode(SCANNER_ELEMENT_ID);
            scannerRef.current = scanner;
            const onDecoded = (decodedText: string) => {
                // Ignora frames repetidos do mesmo código — só processa o primeiro acerto
                // (ver comentário de `hasDecodedRef` acima).
                if (hasDecodedRef.current) return;
                hasDecodedRef.current = true;
                const finishSubmit = () => {
                    setScanning(false);
                    void doSubmit(scanMethod, { scannedValue: decodedText });
                };
                try {
                    void scanner.stop().catch(() => undefined).finally(finishSubmit);
                } catch {
                    // stop() pode lançar de forma síncrona se o scanner já estiver
                    // parando (ex.: outra chamada concorrente) — segue mesmo assim com
                    // o valor já lido, em vez de deixar a Promise rejeitar sem tratamento.
                    finishSubmit();
                }
            };
            const onFrame = () => { /* frame sem leitura — ignora e continua tentando */ };
            try {
                // Prioriza a câmera traseira (celular) — é a que faz sentido pra escanear um
                // código físico. Em desktops/notebooks só existe uma câmera (frontal) e o
                // navegador pode rejeitar essa restrição com OverconstrainedError.
                await scanner.start({ facingMode: "environment" }, { fps: 10, qrbox: { width: 240, height: 240 } }, onDecoded, onFrame);
            } catch {
                await scanner.start({ facingMode: "user" }, { fps: 10, qrbox: { width: 240, height: 240 } }, onDecoded, onFrame);
            }
        } catch (err) {
            setScanning(false);
            setError(cameraErrorMessage(err));
        }
    };

    const stopScan = () => {
        safeStopScanner(scannerRef);
        setScanning(false);
    };

    return (
        <div style={{ display: "grid", gap: 16, textAlign: "center" }}>
            <div>
                <h3 style={{ margin: "0 0 6px", color: "#fff", fontSize: "1.15rem" }}>Confirme para continuar</h3>
                <p style={{ margin: 0, color: "#a8a8b3", fontSize: "0.9rem" }}>{subtitle}</p>
            </div>

            {/*
              O container do scanner precisa existir no DOM ANTES de `new Html5Qrcode(id)`
              rodar — a biblioteca faz `document.getElementById(id)` no construtor e lança
              uma exceção síncrona (fora do try/catch de startScan) se não encontrar. Como
              `setScanning(true)` e a criação do scanner acontecem no mesmo ciclo síncrono,
              o React ainda não commitou o novo DOM nesse instante se o `<div>` só existisse
              condicionalmente — por isso ele fica sempre montado, só escondido via CSS
              quando não está escaneando (em vez de escondido do DOM inteiro).
            */}
            <div id={SCANNER_ELEMENT_ID} style={{ width: "100%", borderRadius: 8, overflow: "hidden", display: scanning ? "block" : "none" }} />
            {scanning ? (
                <button type="button" onClick={stopScan} className="btn-ghost">Cancelar leitura</button>
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
                            style={{ padding: 14, borderRadius: 8, border: "1px solid #323238", backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold", cursor: submitting ? "not-allowed" : "pointer" }}
                        >
                            {requirement.isQrCodeEnabled ? "▦ Escanear QR Code" : "▥ Escanear código de barras"}
                        </button>
                    )}
                    <button type="button" onClick={onCancel} disabled={submitting} className="btn-ghost">
                        Cancelar
                    </button>
                </div>
            )}

            {submitting && <p style={{ color: "#a8a8b3", fontSize: "0.9rem" }}>Confirmando…</p>}
            {error && <p style={{ color: "#ef4444", fontSize: "0.9rem" }}>{error}</p>}
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

/** Trava antes de consultar/lançar numa COMANDA — comprova a leitura daquele código. */
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

/**
 * Trava antes de liberar qualquer pedido direto na MESA (sem comanda — usada quando
 * "Visualização do Cliente (QR Code)" está desligada). Sem código de comanda: a mesa já
 * é identificada pelo próprio token do QR Code, e é isso que o backend usa para
 * registrar a comprovação (ver `ValidateTableReadingCommandHandler`, que grava o
 * número da mesa na trilha de auditoria).
 */
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

/**
 * Usada no lugar do seletor manual "Mesa ou Comanda?" quando "Visualização do Cliente
 * (QR Code)" está desligada e a câmera está ligada (IsCameraInputEnabled): ao clicar em
 * "Pedir", em vez de perguntar onde anotar, o cliente precisa VINCULAR o pedido a uma
 * comanda de verdade — por QR Code ou código de barras (o valor lido JÁ é o número da
 * comanda, sem digitação — IsQrCodeEnabled/IsBarcodeEnabled) ou, quando só a câmera
 * está ligada (sem QR/Barcode), digitando o número manualmente + tirando uma foto como
 * comprovação. Em qualquer um dos casos, o pedido resultante vai pra conta da COMANDA
 * (nunca da mesa) — ver AddPublicOrderItemCommand/Fase 7 no doc do projeto. Uma vez
 * vinculado nesta visita, `PublicOrderPage` guarda o código e pula esta tela nos
 * pedidos seguintes.
 */
export function LinkComandaValidation({ token, requirement, onLinked, onCancel }: LinkComandaProps) {
    const wantsScan = requirement.isBarcodeEnabled || requirement.isQrCodeEnabled;
    const scanMethod = requirement.isQrCodeEnabled ? "qrcode" : "barcode";

    const [manualCode, setManualCode] = useState("");
    const [scanning, setScanning] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const fileInputRef = useRef<HTMLInputElement>(null);
    const scannerRef = useRef<Html5Qrcode | null>(null);
    // Mesma trava de ReadingValidationGate.startScan: o scanner chama o callback de
    // sucesso de novo a cada frame com o código ainda visível, mesmo depois do primeiro
    // acerto (stop() é assíncrono) — sem isso, um único escaneamento podia chamar
    // linkComanda (e abrir/lançar no pedido) mais de uma vez.
    const hasDecodedRef = useRef(false);

    useEffect(() => {
        return () => {
            safeStopScanner(scannerRef);
        };
    }, []);

    const linkComanda = async (code: string, method: "camera" | "barcode" | "qrcode", payload: SubmitPayload) => {
        setSubmitting(true);
        setError(null);
        try {
            // Reaproveita o mesmo endpoint de auditoria da Fase 4 — confirma que a
            // comanda existe (falha com Comanda.NotFound se o código lido/digitado for
            // inválido) e grava a trilha, antes de liberar o vínculo.
            await validateComandaReading(token, code, { method, ...payload });
            onLinked(code);
        } catch {
            setError("Não foi possível vincular a comanda. Confira o número/código e tente de novo.");
        } finally {
            setSubmitting(false);
        }
    };

    const handlePhotoSelected = (file: File) => {
        if (!manualCode.trim()) {
            setError("Informe o número da comanda antes de tirar a foto.");
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
        setError(null);
        setScanning(true);
        hasDecodedRef.current = false;
        try {
            // `new Html5Qrcode(id)` faz `document.getElementById(id)` e lança se não achar —
            // por isso fica dentro do try/catch (ver comentário equivalente em
            // ReadingValidationGate.startScan).
            const scanner = new Html5Qrcode(LINK_SCANNER_ELEMENT_ID);
            scannerRef.current = scanner;
            const onDecoded = (decodedText: string) => {
                // Ignora frames repetidos do mesmo código — só processa o primeiro acerto
                // (ver comentário de `hasDecodedRef` acima).
                if (hasDecodedRef.current) return;
                hasDecodedRef.current = true;
                const finishLink = () => {
                    setScanning(false);
                    // O valor decodificado do QR/código de barras da comanda física É o
                    // número da comanda — sem digitação, ver descrição do componente acima.
                    void linkComanda(decodedText, scanMethod, { scannedValue: decodedText });
                };
                try {
                    void scanner.stop().catch(() => undefined).finally(finishLink);
                } catch {
                    // stop() pode lançar de forma síncrona se o scanner já estiver
                    // parando — segue mesmo assim com o valor já lido.
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
            setError(cameraErrorMessage(err));
        }
    };

    const stopScan = () => {
        safeStopScanner(scannerRef);
        setScanning(false);
    };

    return (
        <div style={{ display: "grid", gap: 16, textAlign: "center" }}>
            <div>
                <h3 style={{ margin: "0 0 6px", color: "#fff", fontSize: "1.15rem" }}>Vincular à comanda</h3>
                <p style={{ margin: 0, color: "#a8a8b3", fontSize: "0.9rem" }}>
                    {wantsScan
                        ? `Escaneie o ${requirement.isQrCodeEnabled ? "QR Code" : "código de barras"} da comanda para lançar este pedido nela.`
                        : "Informe o número da comanda e tire uma foto dela para lançar este pedido nela."}
                </p>
            </div>

            {/* Ver comentário equivalente em ReadingValidationGate: o container precisa
              estar sempre montado (só escondido via CSS) pro `new Html5Qrcode(id)` achar
              o elemento no instante em que o botão é clicado. */}
            <div id={LINK_SCANNER_ELEMENT_ID} style={{ width: "100%", borderRadius: 8, overflow: "hidden", display: scanning ? "block" : "none" }} />
            {scanning ? (
                <button type="button" onClick={stopScan} className="btn-ghost">Cancelar leitura</button>
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
                                style={{ width: "100%", padding: "14px 16px", borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", fontSize: "1rem", outline: "none", boxSizing: "border-box" }}
                            />
                            <input
                                ref={fileInputRef}
                                type="file"
                                accept="image/*"
                                capture="user"
                                style={{ display: "none" }}
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
                            style={{ padding: 14, borderRadius: 8, border: "1px solid #323238", backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold", cursor: submitting ? "not-allowed" : "pointer" }}
                        >
                            {requirement.isQrCodeEnabled ? "▦ Escanear QR Code da comanda" : "▥ Escanear código de barras da comanda"}
                        </button>
                    )}
                    <button type="button" onClick={onCancel} disabled={submitting} className="btn-ghost">
                        Cancelar
                    </button>
                </div>
            )}

            {submitting && <p style={{ color: "#a8a8b3", fontSize: "0.9rem" }}>Vinculando…</p>}
            {error && <p style={{ color: "#ef4444", fontSize: "0.9rem" }}>{error}</p>}
        </div>
    );
}
