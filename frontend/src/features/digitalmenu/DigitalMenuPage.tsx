import { useState, useMemo } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getMenu, getCategories } from "../catalog/api";
import { useAuthStore } from "../../stores/authStore";
import { formatBRL } from "../../lib/types";
import type { MenuItemResponse } from "../../lib/types";
import { Button } from "../../ui/Button";
import { Modal } from "../../ui/Modal";
import { SkeletonList } from "../../ui/Skeleton";
import { useToast } from "../../ui/Toast";

interface CartItem {
    id: string;
    product: MenuItemResponse;
    quantity: number;
    notes: string;
    totalPrice: number;
}

export function DigitalMenuPage() {
    const { mesa } = useParams();
    const toast = useToast();
    const { companyId } = useAuthStore();

    const [activeCategoryId, setActiveCategoryId] = useState<number | null>(null);
    const [selectedProduct, setSelectedProduct] = useState<MenuItemResponse | null>(null);
    const [cart, setCart] = useState<CartItem[]>([]);
    const [isCartOpen, setIsCartOpen] = useState(false);

    const menuQuery = useQuery({
        queryKey: ["menu", companyId],
        queryFn: () => getMenu(companyId ?? 1),
    });

    const categoriesQuery = useQuery({
        queryKey: ["categories", companyId],
        queryFn: () => getCategories(companyId ?? 1),
    });

    // Define a primeira categoria como ativa caso nenhuma esteja selecionada
    const currentCategoryId = activeCategoryId ?? categoriesQuery.data?.[0]?.id ?? null;
    const activeCategoryName = categoriesQuery.data?.find(c => c.id === currentCategoryId)?.name ?? "Cardápio";

    const filteredMenu = useMemo(() => {
        if (!currentCategoryId || !menuQuery.data) return [];
        return menuQuery.data.filter((p) => p.categoryId === currentCategoryId);
    }, [menuQuery.data, currentCategoryId]);

    const cartTotal = cart.reduce((acc, item) => acc + item.totalPrice, 0);

    const handleAddToCart = (item: CartItem) => {
        setCart((prev) => [...prev, item]);
        setSelectedProduct(null);
        toast.success("Item adicionado ao pedido!");
    };

    const handleCallWaiter = () => {
        toast.success("Garçom chamado para a mesa " + mesa);
    };

    return (
        <>
            <style>{`
        .dm-layout {
          display: flex;
          height: 100vh;
          background-color: #ffffff;
          font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
          overflow: hidden;
        }
        
        /* Tema Escuro - Sidebar Tablet */
        .dm-sidebar {
          width: 280px;
          background-color: #111111;
          color: #ffffff;
          display: flex;
          flex-direction: column;
          flex-shrink: 0;
        }
        
        .dm-logo-container {
          padding: 30px 24px;
        }
        
        .dm-logo {
          font-size: 2.2rem;
          font-weight: bold;
          margin: 0;
          letter-spacing: -1px;
        }
        
        .dm-logo span {
          color: #ff6b00;
        }
        
        .dm-subtitle {
          color: #ff6b00;
          font-size: 0.65rem;
          letter-spacing: 1px;
          margin-top: 4px;
          text-transform: uppercase;
          font-weight: bold;
        }

        .dm-nav-item {
          padding: 16px 24px;
          display: flex;
          align-items: center;
          justify-content: space-between;
          cursor: pointer;
          font-weight: 500;
          border-left: 4px solid transparent;
          transition: all 0.2s;
        }
        .dm-nav-item.active {
          color: #ff6b00;
          border-left-color: #ff6b00;
          background-color: rgba(255, 107, 0, 0.05);
        }

        /* Área de Conteúdo Branca */
        .dm-main {
          flex: 1;
          display: flex;
          flex-direction: column;
          overflow: hidden;
        }

        /* Header Desktop/Tablet */
        .dm-header-desktop {
          display: flex;
          justify-content: flex-end;
          align-items: center;
          padding: 24px 40px;
          gap: 20px;
        }

        /* Header Mobile */
        .dm-header-mobile {
          display: none;
          background-color: #111111;
          color: #ffffff;
          padding: 40px 20px 20px 20px;
          flex-direction: column;
        }

        .dm-btn-outline {
          border: 1px solid #ff6b00;
          color: #ff6b00;
          background: transparent;
          border-radius: 30px;
          padding: 8px 16px;
          font-weight: bold;
          font-size: 0.85rem;
          cursor: pointer;
          display: flex;
          align-items: center;
          gap: 8px;
        }

        .dm-btn-outline.dark-text {
          color: #111111;
        }
        
        .dm-badge {
          background-color: #ff6b00;
          color: #ffffff;
          border-radius: 50%;
          width: 20px;
          height: 20px;
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 0.75rem;
        }

        .dm-content-scroll {
          flex: 1;
          overflow-y: auto;
          padding: 0 40px 40px 40px;
        }

        .dm-title {
          font-size: 2rem;
          font-weight: 800;
          text-transform: uppercase;
          margin: 0 0 4px 0;
          color: #111111;
        }

        .dm-desc {
          color: #666666;
          margin: 0 0 24px 0;
          font-size: 0.95rem;
        }

        /* Pills Horizontais */
        .dm-pills-container {
          display: flex;
          gap: 12px;
          overflow-x: auto;
          padding-bottom: 24px;
          scrollbar-width: none;
        }
        .dm-pills-container::-webkit-scrollbar { display: none; }
        
        .dm-pill {
          background-color: #f5f5f5;
          color: #333333;
          border: none;
          border-radius: 20px;
          padding: 8px 24px;
          font-weight: 600;
          font-size: 0.85rem;
          white-space: nowrap;
          cursor: pointer;
        }
        .dm-pill.active {
          background-color: #ff6b00;
          color: #ffffff;
        }

        /* Grid de Produtos */
        .dm-grid {
          display: grid;
          grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
          gap: 20px;
          margin-bottom: 40px;
        }

        /* Card de Produto Idêntico ao Protótipo */
        .dm-card {
          display: flex;
          background: #ffffff;
          border: 1px solid #f0f0f0;
          border-radius: 16px;
          padding: 16px;
          gap: 16px;
          box-shadow: 0 4px 15px rgba(0, 0, 0, 0.03);
          cursor: pointer;
          transition: transform 0.2s, box-shadow 0.2s;
        }
        .dm-card:active {
          transform: scale(0.98);
        }

        .dm-card-img {
          width: 80px;
          height: 100px;
          object-fit: contain;
          border-radius: 8px;
        }

        .dm-card-info {
          flex: 1;
          display: flex;
          flex-direction: column;
          justify-content: space-between;
        }

        .dm-card-add-btn {
          border: 1px solid #ff6b00;
          background: transparent;
          color: #111111;
          border-radius: 20px;
          padding: 4px 14px;
          font-size: 0.75rem;
          font-weight: bold;
          display: flex;
          align-items: center;
          gap: 6px;
          cursor: pointer;
        }

        /* Rodapé de Informações */
        .dm-footer-info {
          display: flex;
          justify-content: space-between;
          border-top: 1px solid #eeeeee;
          padding-top: 24px;
          color: #666666;
          font-size: 0.85rem;
        }
        .dm-footer-info strong {
          color: #111111;
          display: block;
          margin-top: 4px;
        }
        
        .dm-bottom-nav {
          display: none;
        }

        /* --- RESPONSIVIDADE PARA CELULAR --- */
        @media (max-width: 768px) {
          .dm-layout {
            flex-direction: column;
          }
          .dm-sidebar {
            display: none;
          }
          .dm-header-desktop {
            display: none;
          }
          .dm-header-mobile {
            display: flex;
          }
          .dm-content-scroll {
            padding: 24px 20px 100px 20px; /* Espaço para a bottom nav */
          }
          .dm-grid {
            grid-template-columns: 1fr;
          }
          .dm-footer-info {
            flex-direction: column;
            gap: 16px;
          }
          
          /* Bottom Nav Mobile */
          .dm-bottom-nav {
            display: flex;
            background-color: #111111;
            position: fixed;
            bottom: 0;
            width: 100%;
            padding: 12px 16px;
            overflow-x: auto;
            gap: 24px;
            scrollbar-width: none;
            z-index: 50;
            border-top: 1px solid #222;
          }
          .dm-bottom-nav::-webkit-scrollbar { display: none; }
          
          .dm-bottom-item {
            display: flex;
            flex-direction: column;
            align-items: center;
            color: #888888;
            min-width: 60px;
            cursor: pointer;
          }
          .dm-bottom-item.active {
            color: #ff6b00;
          }
          .dm-bottom-item svg {
            margin-bottom: 6px;
          }
          .dm-bottom-item span {
            font-size: 0.65rem;
            text-transform: uppercase;
            font-weight: bold;
          }
        }
      `}</style>

            <div className="dm-layout">

                {/* SIDEBAR (Desktop/Tablet) */}
                <aside className="dm-sidebar">
                    <div className="dm-logo-container">
                        <h1 className="dm-logo">ding<span>.food</span></h1>
                        <div className="dm-subtitle">— Gestão inteligente para restaurantes —</div>
                    </div>
                    <div style={{ flex: 1, overflowY: "auto" }}>
                        {categoriesQuery.data?.map((cat) => (
                            <div
                                key={cat.id}
                                className={`dm-nav-item ${currentCategoryId === cat.id ? "active" : ""}`}
                                onClick={() => setActiveCategoryId(cat.id)}
                            >
                                <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                                    <IconPlaceholder /> {cat.name}
                                </div>
                                <span>›</span>
                            </div>
                        ))}
                    </div>
                    {/* Banner Promo */}
                    <div style={{ margin: "20px", padding: "16px", border: "1px solid #333", borderRadius: 12, display: "flex", alignItems: "center", gap: 12 }}>
                        <div style={{ fontSize: "2rem" }}>🛎️</div>
                        <div>
                            <div style={{ fontSize: "0.75rem", color: "#aaa" }}>PEÇA PELO APP E GANHE</div>
                            <div style={{ fontSize: "1.2rem", color: "#ff6b00", fontWeight: "bold" }}>5% <span style={{ fontSize: "0.75rem", color: "#aaa" }}>DE DESCONTO!</span></div>
                        </div>
                    </div>
                </aside>

                <main className="dm-main">

                    {/* HEADER DESKTOP */}
                    <header className="dm-header-desktop">
                        <div style={{ textAlign: "right" }}>
                            <div style={{ color: "#ff6b00", fontSize: "0.85rem", fontWeight: "bold" }}>MESA {mesa}</div>
                            <div style={{ color: "#666", fontSize: "0.85rem" }}>Convidado</div>
                        </div>
                        <button className="dm-btn-outline" onClick={handleCallWaiter}>
                            🛎️ CHAMAR GARÇOM
                        </button>
                        <button className="dm-btn-outline dark-text" onClick={() => setIsCartOpen(true)}>
                            🛒 MEU PEDIDO <div className="dm-badge">{cart.length}</div>
                        </button>
                    </header>

                    {/* HEADER MOBILE */}
                    <header className="dm-header-mobile">
                        <div style={{ textAlign: "center", marginBottom: 24 }}>
                            <h1 className="dm-logo" style={{ color: "#fff" }}>ding<span>.food</span></h1>
                            <div className="dm-subtitle" style={{ justifyContent: "center", display: "flex" }}>Gestão inteligente para restaurantes</div>
                        </div>
                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                            <div>
                                <div style={{ color: "#ff6b00", fontSize: "0.85rem", fontWeight: "bold", textTransform: "uppercase" }}>MESA {mesa}</div>
                                <div style={{ color: "#aaa", fontSize: "0.85rem" }}>Convidado</div>
                            </div>
                            <div style={{ display: "flex", gap: 8 }}>
                                <button className="dm-btn-outline" style={{ padding: "6px 12px", fontSize: "0.75rem" }} onClick={handleCallWaiter}>
                                    🛎️
                                </button>
                                <button className="dm-btn-outline" style={{ color: "#fff", padding: "6px 12px", fontSize: "0.75rem" }} onClick={() => setIsCartOpen(true)}>
                                    🛒 MEU PEDIDO <div className="dm-badge">{cart.length}</div>
                                </button>
                            </div>
                        </div>
                    </header>

                    {/* CONTEÚDO */}
                    <div className="dm-content-scroll">
                        <h2 className="dm-title">{activeCategoryName}</h2>
                        <p className="dm-desc">Refrescância e sabor para todos os momentos</p>

                        {/* Sub-filtros (Pills mockados para visualização do protótipo) */}
                        <div className="dm-pills-container">
                            <button className="dm-pill active">TODOS</button>
                            <button className="dm-pill">ÁGUAS</button>
                            <button className="dm-pill">REFRIGERANTES</button>
                            <button className="dm-pill">SUCOS</button>
                            <button className="dm-pill">CERVEJAS</button>
                        </div>

                        {/* Grid de Produtos */}
                        {menuQuery.isLoading ? (
                            <SkeletonList rows={4} rowHeight={120} />
                        ) : (
                            <div className="dm-grid">
                                {filteredMenu.map((product) => (
                                    <div key={product.id} className="dm-card" onClick={() => setSelectedProduct(product)}>
                                        <img
                                            src={product.imageUrl || "https://via.placeholder.com/80x100?text=Sem+Foto"}
                                            alt={product.name}
                                            className="dm-card-img"
                                        />
                                        <div className="dm-card-info">
                                            <div>
                                                <h4 style={{ margin: "0 0 4px 0", fontSize: "0.95rem", color: "#111", fontWeight: "bold" }}>
                                                    {product.name}
                                                </h4>
                                                <div style={{ fontSize: "0.75rem", color: "#888", display: "-webkit-box", WebkitLineClamp: 2, WebkitBoxOrient: "vertical", overflow: "hidden" }}>
                                                    {product.description || "350ml"}
                                                </div>
                                            </div>
                                            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginTop: 12 }}>
                                                <div style={{ color: "#ff6b00", fontWeight: "bold", fontSize: "1rem" }}>
                                                    {formatBRL(product.salePrice)}
                                                </div>
                                                <button className="dm-card-add-btn">
                                                    🛒 ADICIONAR
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        )}

                        {/* Informações do Rodapé */}
                        <div className="dm-footer-info">
                            <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
                                <span style={{ fontSize: "1.5rem", color: "#ff6b00" }}>⏱</span>
                                <div>TEMPO MÉDIO DE PREPARO <strong>25 a 35 min</strong></div>
                            </div>
                            <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
                                <span style={{ fontSize: "1.5rem", color: "#ff6b00" }}>🛵</span>
                                <div>TAXA DE ENTREGA <strong>R$ 6,90</strong></div>
                            </div>
                            <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
                                <span style={{ fontSize: "1.5rem", color: "#ff6b00" }}>💳</span>
                                <div>FORMAS DE PAGAMENTO <strong>Crédito, Débito e Pix</strong></div>
                            </div>
                        </div>
                    </div>
                </main>

                {/* BOTTOM NAV (Apenas Mobile) */}
                <nav className="dm-bottom-nav">
                    {categoriesQuery.data?.map((cat) => (
                        <div
                            key={cat.id}
                            className={`dm-bottom-item ${currentCategoryId === cat.id ? "active" : ""}`}
                            onClick={() => setActiveCategoryId(cat.id)}
                        >
                            <IconPlaceholder />
                            <span>{cat.name}</span>
                        </div>
                    ))}
                </nav>
            </div>

            {/* MODAL DE PRODUTO */}
            {selectedProduct && (
                <ProductOrderModal
                    product={selectedProduct}
                    onClose={() => setSelectedProduct(null)}
                    onAdd={handleAddToCart}
                />
            )}

            {/* MODAL DO CARRINHO (Simples) */}
            {isCartOpen && (
                <Modal title="Seu Pedido" onClose={() => setIsCartOpen(false)}>
                    <div style={{ minHeight: 200 }}>
                        {cart.length === 0 ? (
                            <p style={{ textAlign: "center", color: "#888", marginTop: 40 }}>Seu pedido está vazio.</p>
                        ) : (
                            <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
                                {cart.map(item => (
                                    <div key={item.id} style={{ display: "flex", justifyContent: "space-between", borderBottom: "1px solid #eee", paddingBottom: 8 }}>
                                        <div>
                                            <strong>{item.quantity}x {item.product.name}</strong>
                                            {item.notes && <div style={{ fontSize: "0.8rem", color: "#666" }}>{item.notes}</div>}
                                        </div>
                                        <strong style={{ color: "#ff6b00" }}>{formatBRL(item.totalPrice)}</strong>
                                    </div>
                                ))}
                                <div style={{ display: "flex", justifyContent: "space-between", fontSize: "1.2rem", fontWeight: "bold", marginTop: 16 }}>
                                    <span>Total</span>
                                    <span style={{ color: "#ff6b00" }}>{formatBRL(cartTotal)}</span>
                                </div>
                                <Button variant="primary" block style={{ background: "#ff6b00", borderColor: "#ff6b00", marginTop: 16 }} onClick={() => {
                                    toast.success("Pedido enviado para a cozinha!");
                                    setCart([]);
                                    setIsCartOpen(false);
                                }}>
                                    Confirmar Pedido
                                </Button>
                            </div>
                        )}
                    </div>
                </Modal>
            )}
        </>
    );
}

// Componente para icone genérico
function IconPlaceholder() {
    return (
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
            <path d="M12 2v20M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
        </svg>
    );
}

// Modal Interno para escolha de quantidades
function ProductOrderModal({ product, onClose, onAdd }: { product: MenuItemResponse, onClose: () => void, onAdd: (item: CartItem) => void }) {
    const [quantity, setQuantity] = useState(1);
    const [notes, setNotes] = useState("");

    const handleConfirm = () => {
        onAdd({
            id: Math.random().toString(36).substr(2, 9),
            product,
            quantity,
            notes,
            totalPrice: product.salePrice * quantity,
        });
    };

    return (
        <Modal title={product.name} onClose={onClose} variant="center">
            {product.imageUrl && (
                <img src={product.imageUrl} alt={product.name} style={{ width: "100%", height: 200, objectFit: "cover", borderRadius: 12, marginBottom: 16 }} />
            )}
            <p style={{ color: "#666", marginBottom: 20 }}>{product.description}</p>

            <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", background: "#f9f9f9", padding: 12, borderRadius: 8 }}>
                    <span style={{ fontWeight: "bold" }}>Quantidade</span>
                    <div style={{ display: "flex", gap: 16, alignItems: "center" }}>
                        <button onClick={() => setQuantity(Math.max(1, quantity - 1))} style={{ width: 32, height: 32, borderRadius: "50%", border: "1px solid #ccc", background: "#fff", fontSize: "1.2rem" }}>-</button>
                        <span style={{ fontSize: "1.2rem", fontWeight: "bold" }}>{quantity}</span>
                        <button onClick={() => setQuantity(quantity + 1)} style={{ width: 32, height: 32, borderRadius: "50%", border: "1px solid #ff6b00", color: "#ff6b00", background: "#fff", fontSize: "1.2rem" }}>+</button>
                    </div>
                </div>

                <textarea
                    placeholder="Alguma observação? (Ex: Sem gelo)"
                    value={notes}
                    onChange={(e) => setNotes(e.target.value)}
                    style={{ width: "100%", height: 80, padding: 12, borderRadius: 8, border: "1px solid #ddd", fontFamily: "inherit" }}
                />

                <Button variant="primary" block onClick={handleConfirm} style={{ background: "#ff6b00", borderColor: "#ff6b00" }}>
                    Adicionar • {formatBRL(product.salePrice * quantity)}
                </Button>
            </div>
        </Modal>
    );
}