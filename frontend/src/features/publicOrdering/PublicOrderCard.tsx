import { formatBRL } from "../../lib/types";
import type { MenuItemResponse } from "../../lib/types";

type PublicOrderCardProps = {
    item: MenuItemResponse;
    quantity: number;
    isJustSent: boolean;
    isPending: boolean;
    onQuantityChange: (newQty: number) => void;
    onAddItem: () => void;
};

export function PublicOrderCard({ item, quantity, isJustSent, isPending, onQuantityChange, onAddItem }: PublicOrderCardProps) {
    return (
        <div style={{ display: "flex", backgroundColor: "#1e1e24", borderRadius: 12, padding: 16, border: "1px solid #29292e", boxShadow: "0 4px 6px rgba(0,0,0,0.3)" }}>
            <div style={{ width: 80, height: 80, borderRadius: 8, backgroundColor: "#323238", flexShrink: 0, overflow: "hidden" }}>
                {item.imageUrl ? (
                    <img src={item.imageUrl} alt={item.name} style={{ width: "100%", height: "100%", objectFit: "cover" }} />
                ) : (
                    <div style={{ width: "100%", height: "100%", display: "flex", alignItems: "center", justifyContent: "center", color: "#555" }}>📷</div>
                )}
            </div>

            <div style={{ marginLeft: 16, flex: 1, display: "flex", flexDirection: "column", justifyContent: "space-between" }}>
                <div>
                    <h3 style={{ margin: 0, fontSize: "1rem", color: "#ffffff", fontWeight: "600" }}>{item.name}</h3>
                    {item.description && (
                        <p style={{ margin: "4px 0 0", fontSize: "0.85rem", color: "#8d8d99", lineHeight: "1.3" }}>
                            {item.description}
                        </p>
                    )}
                </div>

                <span style={{ marginTop: 12, fontWeight: "bold", color: "#f59e0b", fontSize: "1.1rem" }}>
                    {item.complementGroups?.length ? "A partir de " : ""}{formatBRL(item.salePrice)}
                </span>

                <div style={{ display: "flex", justifyContent: "flex-end", alignItems: "center", gap: 12, marginTop: 12 }}>
                    <div style={{ display: "flex", alignItems: "center", border: "1px solid #323238", borderRadius: 8, overflow: "hidden", height: 36, backgroundColor: "#121214" }}>
                        <button type="button" onClick={() => onQuantityChange(quantity - 1)} style={{ width: 36, height: "100%", background: "none", border: "none", color: "#a8a8b3", fontSize: "1.2rem", cursor: "pointer" }}>−</button>
                        <span style={{ width: 28, textAlign: "center", color: "#ffffff", fontWeight: "500", fontSize: "0.95rem" }}>{quantity}</span>
                        <button type="button" onClick={() => onQuantityChange(quantity + 1)} style={{ width: 36, height: "100%", background: "none", border: "none", color: "#a8a8b3", fontSize: "1.2rem", cursor: "pointer" }}>+</button>
                    </div>

                    <button
                        type="button"
                        onClick={onAddItem}
                        disabled={isPending}
                        style={{ backgroundColor: "#f59e0b", color: "#121214", border: "none", borderRadius: 8, padding: "0 20px", height: 36, fontWeight: "bold", fontSize: "0.95rem", cursor: isPending ? "not-allowed" : "pointer", opacity: isPending ? 0.7 : 1 }}
                    >
                        {isJustSent ? "Pedir de novo" : "Pedir"}
                    </button>
                </div>
            </div>
        </div>
    );
}