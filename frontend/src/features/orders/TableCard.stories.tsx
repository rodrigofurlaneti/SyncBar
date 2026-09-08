import { TableCard, TableCardSkeleton } from "./TableCard";
import { TableStatus } from "../../lib/types";

export default { title: "Salão/Mesas", component: TableCard,
    args: { id: 1, number: 4, capacity: 4, onOpen: () => undefined } };
export const Livre = { args: { statusId: TableStatus.Livre } };
export const Ocupada = { args: { statusId: TableStatus.Ocupada, itemsCount: 3, totalValue: 49.9, openedAt: "2026-09-08T12:30:00" } };
export const Fechando = { args: { statusId: TableStatus.EmFechamento, itemsCount: 3, totalValue: 49.9 } };
export const Indisponivel = { args: { statusId: TableStatus.Interditada, disabled: true } };
export const Carregando = { render: () => <TableCardSkeleton /> };
