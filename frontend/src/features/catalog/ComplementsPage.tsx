import { useState } from "react";
import { ComplementItemsPanel } from "./ComplementItemsPanel";
import { ComplementGroupsPanel } from "./ComplementGroupsPanel";

type Tab = "groups" | "items";

export function ComplementsPage() {
  const [tab, setTab] = useState<Tab>("groups");

  return (
    <main style={{ padding: 22, maxWidth: 900, margin: "0 auto" }}>
      <div className="rise" style={{ display: "flex", alignItems: "baseline", gap: 14, marginBottom: 16 }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>
          Complementos
        </h2>
      </div>

      <div className="rise rise-1" style={{ display: "flex", gap: 8, marginBottom: 18 }}>
        <button
          type="button"
          className={tab === "groups" ? "btn-primary" : "btn-ghost"}
          onClick={() => setTab("groups")}
        >
          Grupos
        </button>
        <button
          type="button"
          className={tab === "items" ? "btn-primary" : "btn-ghost"}
          onClick={() => setTab("items")}
        >
          Itens
        </button>
      </div>

      {tab === "groups" ? <ComplementGroupsPanel /> : <ComplementItemsPanel />}
    </main>
  );
}
