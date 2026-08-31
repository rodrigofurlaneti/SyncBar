import { useState, useEffect } from "react";
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
    const [windowWidth, setWindowWidth] = useState(typeof window !== "undefined" ? window.innerWidth : 1200);

    useEffect(() => {
        const handleResize = () => setWindowWidth(window.innerWidth);
        window.addEventListener("resize", handleResize);
        return () => window.removeEventListener("resize", handleResize);
    }, []);

    const isTvOrLarge = windowWidth > 1200;

    // Dimensões dinâmicas baseadas na tela
    const cardPadding = isTvOrLarge ? 24 : 16;
    const imageSize = isTvOrLarge ? 120 : 80;
    const titleFontSize = isTvOrLarge ? "1.25rem" : "1rem";
    const descFontSize = isTvOrLarge ? "1.05rem" : "0.85rem";
    const priceFontSize = isTvOrLarge ? "1.35rem" : "1.1rem";
    const controlHeight = isTvOrLarge ? 48 : 36;
    const buttonPadding = isTvOrLarge ? "0 28px" : "0 20px";

    return (
        <div style={{ display: "flex", backgroundColor: "#1e1e24", borderRadius: 12, padding: cardPadding, border: "1px solid #29292e", boxShadow: "0 4px 6px rgba(0,0,0,0.3)", boxSizing: "border-box" }}>
            <div style={{ width: imageSize, height: imageSize, borderRadius: 8, backgroundColor: "#323238", flexShrink: 0, overflow: "hidden" }}>
                {item.imageUrl ? (
                    <img src={item.imageUrl} alt={item.name} style={{ width: "100%", height: "100%", objectFit: "cover" }} />
                ) : (
                    <div style={{ width: "100%", height: "100%", display: "flex", alignItems: "center", justifyContent: "center", color: "#555", fontSize: isTvOrLarge ? "2rem" : "1.2rem" }}>📷</div>
                )}
            </div>

            <div style={{ marginLeft: 16, flex: 1, display: "flex", flexDirection: "column", justifyContent: "space-between" }}>
                <div>
                    <h3 style={{ margin: 0, fontSize: titleFontSize, color: "#ffffff", fontWeight: "600", lineHeight: "1.2" }}>{item.name}</h3>
                    {item.description && (
                        <p style={{ margin: "6px 0 0", fontSize: descFontSize, color: "#8d8d99", lineHeight: "1.4" }}>
                            {item.description}
                        </p>
                    )}
                </div>

                <span style={{ marginTop: 12, fontWeight: "bold", color: "#f59e0b", fontSize: priceFontSize }}>
                    {item.complementGroups?.length ? "A partir de " : ""}{formatBRL(item.salePrice)}
                </span>

                <div style={{ display: "flex", justifyContent: "flex-end", alignItems: "center", gap: 12, marginTop: 12 }}>
                    <div style={{ display: "flex", alignItems: "center", border: "1px solid #323238", borderRadius: 8, overflow: "hidden", height: controlHeight, backgroundColor: "#121214" }}>
                        <button type="button" onClick={() => onQuantityChange(quantity - 1)} style={{ width: controlHeight, height: "100%", background: "none", border: "none", color: "#a8a8b3", fontSize: isTvOrLarge ? "1.5rem" : "1.2rem", cursor: "pointer" }}>−</button>
                        <span style={{ width: isTvOrLarge ? 40 : 28, textAlign: "center", color: "#ffffff", fontWeight: "500", fontSize: isTvOrLarge ? "1.15rem" : "0.95rem" }}>{quantity}</span>
                        <button type="button" onClick={() => onQuantityChange(quantity + 1)} style={{ width: controlHeight, height: "100%", background: "none", border: "none", color: "#a8a8b3", fontSize: isTvOrLarge ? "1.5rem" : "1.2rem", cursor: "pointer" }}>+</button>
                    </div>

                    <button
                        type="button"
                        onClick={onAddItem}
                        disabled={isPending}
                        style={{ backgroundColor: "#f59e0b", color: "#121214", border: "none", borderRadius: 8, padding: buttonPadding, height: controlHeight, fontWeight: "bold", fontSize: isTvOrLarge ? "1.1rem" : "0.95rem", cursor: isPending ? "not-allowed" : "pointer", opacity: isPending ? 0.7 : 1 }}
                    >
                        {isJustSent ? "Pedir de novo" : "Pedir"}
                    </button>
                </div>
            </div>
        </div>
    );
}