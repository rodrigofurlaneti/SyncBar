import { useCashRegister } from "./useCashRegister";

export function CashRegisterField({ branchId }: { branchId: number }) {
    const cash = useCashRegister(branchId);
    if (cash.isError) return <p role="alert" className="error-text">Não foi possível consultar os terminais. <button type="button" onClick={() => void cash.refetch()}>Tentar novamente</button></p>;
    if (cash.isLoading) return <p role="status">Carregando terminais…</p>;
    if (!cash.registerId) return <p role="alert">Cadastre um terminal na Gestão de Caixa desta filial.</p>;
    return <label style={{ display: "grid", gap: 4 }}>Caixa do recebimento<select value={cash.registerId} onChange={e => cash.selectRegister(Number(e.target.value))}>{cash.registers.map(r => <option key={r.id} value={r.id}>{r.name}</option>)}</select></label>;
}
