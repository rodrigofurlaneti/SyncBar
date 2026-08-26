import { useMemo } from "react";
import { QueryError } from "../../../../components/QueryError";
export interface WaiterMessageResponse {
    id: number;
    branchId: number;
    senderEmployeeId: number;
    recipientEmployeeId: number | null;
    diningAreaId: number | null;
    message: string;
    isRead: boolean;
    createdAt: string;
}
interface TabMensagensProps {
    activeAreaId: number | null;
    isLoading: boolean;
    isError: boolean;
    error: unknown;
    messages: WaiterMessageResponse[];
}
export function TabMensagens({ activeAreaId, isLoading, isError, error, messages }: TabMensagensProps) {
    const sortedMessages = useMemo(() => {
        return [...messages].sort((a, b) =>
            new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        );
    }, [messages]);
    return (
        <section className="waiter-section">
            <h2 className="waiter-section-title" style={{ marginBottom: 16 }}>Mensagens e Avisos</h2>

            {!activeAreaId ? (
                <p className="waiter-empty">Atribua-se a uma praça para visualizar as mensagens locais.</p>
            ) : isLoading ? (
                <p className="waiter-empty">Carregando mensagens...</p>
            ) : isError ? (
                <QueryError error={error} what="as mensagens" />
            ) : sortedMessages.length === 0 ? (
                <p className="waiter-empty">Nenhuma mensagem registrada na sua praça no momento.</p>
            ) : (
                <div className="waiter-order-list" style={{ display: "grid", gap: "10px" }}>
                    {sortedMessages.map((msg) => (
                        <div
                            key={msg.id}
                            style={{
                                backgroundColor: "var(--surface, #ffffff)",
                                borderRadius: "10px",
                                padding: "14px",
                                border: "1px solid var(--border, #e5e7eb)",
                                borderLeft: `6px solid ${msg.isRead ? "#9ca3af" : "#3b82f6"}`,
                                display: "grid",
                                gap: "6px"
                            }}
                        >
                            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                                <span style={{ fontSize: "0.75rem", fontWeight: "700", color: "var(--ink-dim)" }}>
                                    Aviso Operacional
                                </span>
                                <span style={{ fontSize: "0.75rem", color: "var(--ink-faint)" }}>
                                    {new Date(msg.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} - {new Date(msg.createdAt).toLocaleDateString()}
                                </span>
                            </div>
                            <p style={{ fontSize: "0.95rem", color: "var(--ink)", margin: 0, fontWeight: 500 }}>
                                {msg.message}
                            </p>
                        </div>
                    ))}
                </div>
            )}
        </section>
    );
}