import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "../../lib/apiClient";
import { Button } from "../../ui/Button";
import { Switch } from "../../ui/Switch";
import { TextField } from "../../ui/Field";
import { formatBRL } from "../../lib/types";

type Flags = { hasOptionalExtras: boolean; hasBoosts: boolean };
type Item = { id: number; productId: number; optionalExtraName?: string; boostName?: string; incrementalValue?: number; displayOrder: number };
type Draft = { id?: number; name: string; value: string; order: string };
const empty: Draft = { name: "", value: "0", order: "0" };

export function ProductExtrasPanel({ productId }: { productId: number }) {
    const client = useQueryClient();
    const key = ["product-extras", productId];
    const query = useQuery({ queryKey: key, queryFn: () => api<Flags>(`/api/products/${productId}`) });
    const toggle = useMutation({
        mutationFn: (flags: Flags) => api<void>(`/api/products/${productId}/extras-and-boosts`, { method: "PUT", body: JSON.stringify(flags) }),
        onSuccess: async () => {
            await client.invalidateQueries({ queryKey: key });
            await client.invalidateQueries({ queryKey: ["menu"] });
        },
    });
    if (query.isPending) return <p>Carregando opcionais e adicionais…</p>;
    if (query.isError) return <p role="alert">Não foi possível carregar os opcionais. <button onClick={() => void query.refetch()}>Tentar novamente</button></p>;
    const flags = query.data;
    return <section aria-label="Opcionais e adicionais do produto" className="ui-stack">
        <p>Alterações nesta seção são salvas imediatamente.</p>
        {(["hasOptionalExtras", "hasBoosts"] as const).map(flag => {
            const label = flag === "hasBoosts" ? "Possui Adicionais Pagos?" : "Possui Opcionais Gratuitos?";
            return <div key={flag}>
                <div className="ui-row">
                    <Switch label={label} checked={flags[flag]} disabled={toggle.isPending} onChange={next => toggle.mutate({ ...flags, [flag]: next })} />
                    <span>{label}</span>
                </div>
                {flags[flag] && <ExtrasList productId={productId} paid={flag === "hasBoosts"} />}
            </div>;
        })}
        {toggle.isError && <p role="alert">{toggle.error.message}</p>}
    </section>;
}

function ExtrasList({ productId, paid }: { productId: number; paid: boolean }) {
    const client = useQueryClient();
    const route = paid ? "boosts" : "optional-extras";
    const url = `/api/products/${productId}/${route}`;
    const key = ["product-extras", productId, route];
    const query = useQuery({ queryKey: key, queryFn: () => api<Item[]>(url) });
    const [draft, setDraft] = useState<Draft>(empty);
    const mutation = useMutation({
        mutationFn: ({ item, remove }: { item: Draft; remove?: boolean }) => api<void>(url + (item.id === undefined ? "" : `/${item.id}`), {
            method: remove ? "DELETE" : item.id === undefined ? "POST" : "PUT",
            body: remove ? undefined : JSON.stringify({
                [paid ? "boostName" : "optionalExtraName"]: item.name.trim(),
                displayOrder: Number(item.order),
                ...(paid ? { incrementalValue: Number(item.value.replace(",", ".")) } : {}),
            }),
        }),
        onSuccess: async () => {
            setDraft(empty);
            await client.invalidateQueries({ queryKey: ["product-extras", productId] });
            await client.invalidateQueries({ queryKey: ["menu"] });
        },
    });
    const value = Number(draft.value.replace(",", "."));
    const valid = draft.name.trim().length > 0 && draft.name.trim().length <= 150 &&
        draft.order.trim() !== "" && Number.isInteger(Number(draft.order)) && Number(draft.order) >= 0 && Number(draft.order) <= 2147483647 &&
        (!paid || (draft.value.trim() !== "" && Number.isFinite(value) && value >= 0 && /^\d+([.,]\d{1,2})?$/.test(draft.value)));
    return <div className="ui-stack">
        {query.isPending && <p>Carregando itens…</p>}
        {query.isError && <p role="alert">Falha ao carregar itens. <button onClick={() => void query.refetch()}>Tentar novamente</button></p>}
        {query.data?.length === 0 && <p>Nenhum item cadastrado.</p>}
        <ul>
            {query.data?.map(item => <li key={item.id} className="ui-row ui-row-wrap">
                <span>{item.displayOrder} · {item.optionalExtraName ?? item.boostName}{paid ? ` · + ${formatBRL(item.incrementalValue ?? 0)}` : " · Grátis"}</span>
                <Button size="sm" disabled={mutation.isPending} onClick={() => setDraft({ id: item.id, name: item.optionalExtraName ?? item.boostName ?? "", value: String(item.incrementalValue ?? 0), order: String(item.displayOrder) })}>Editar</Button>
                <Button size="sm" variant="danger" disabled={mutation.isPending} onClick={() => mutation.mutate({ item: { ...empty, id: item.id }, remove: true })}>Excluir</Button>
            </li>)}
        </ul>
        <TextField label={paid ? "Nome do adicional" : "Nome do opcional"} maxLength={150} value={draft.name} onChange={e => setDraft({ ...draft, name: e.target.value })} />
        {paid && <TextField label="Valor adicional (R$)" inputMode="decimal" value={draft.value} onChange={e => setDraft({ ...draft, value: e.target.value })} />}
        <TextField label="Ordem de exibição" type="number" min={0} step={1} value={draft.order} onChange={e => setDraft({ ...draft, order: e.target.value })} />
        <div className="ui-row">
            <Button disabled={!valid || query.isError || query.isPending || mutation.isPending} onClick={() => mutation.mutate({ item: draft })}>{draft.id === undefined ? "Adicionar item" : "Salvar item"}</Button>
            {draft.id !== undefined && <Button disabled={mutation.isPending} onClick={() => setDraft(empty)}>Cancelar edição</Button>}
        </div>
        {mutation.isError && <p role="alert">{mutation.error.message}</p>}
    </div>;
}

