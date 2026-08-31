import { useState } from "react";

export function CalculatorModal({ onClose }: { onClose: () => void }) {
    const [calcInput, setCalcInput] = useState("0");
    const [calcPrevValue, setCalcPrevValue] = useState<number | null>(null);
    const [calcOperator, setCalcOperator] = useState<string | null>(null);
    const [calcNewNumber, setCalcNewNumber] = useState(false);

    const handleCalcNum = (num: string) => {
        if (calcNewNumber) {
            setCalcInput(num);
            setCalcNewNumber(false);
        } else {
            setCalcInput(calcInput === "0" && num !== "." ? num : calcInput + num);
        }
    };

    const handleCalcOp = (op: string) => {
        if (calcOperator && !calcNewNumber) handleCalcEqual();
        else setCalcPrevValue(parseFloat(calcInput));
        setCalcOperator(op);
        setCalcNewNumber(true);
    };

    const handleCalcEqual = () => {
        if (calcOperator && calcPrevValue !== null) {
            const current = parseFloat(calcInput);
            let result = 0;
            if (calcOperator === "+") result = calcPrevValue + current;
            if (calcOperator === "-") result = calcPrevValue - current;
            if (calcOperator === "*") result = calcPrevValue * current;
            if (calcOperator === "/") result = calcPrevValue / current;
            setCalcInput(parseFloat(result.toFixed(4)).toString());
            setCalcPrevValue(null);
            setCalcOperator(null);
            setCalcNewNumber(true);
        }
    };

    return (
        <div
            className="modal-backdrop is-center"
            onMouseDown={(e) => {
                if (e.target === e.currentTarget) onClose();
            }}
            style={{ position: "absolute" }}
        >
            <div className="modal-panel is-center" style={{ width: "90%", maxWidth: "320px", padding: "20px" }}>
                <div className="modal-head" style={{ marginBottom: "16px", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                    <span className="display" style={{ fontSize: "1.2rem", fontWeight: "bold" }}>➗ Calculadora</span>
                    <button type="button" className="btn-ghost btn-icon" onClick={onClose}>✕</button>
                </div>
                <div style={{ backgroundColor: "var(--bg-body)", border: "1px solid var(--border)", borderRadius: "8px", padding: "16px", fontSize: "2rem", textAlign: "right", marginBottom: "16px", overflow: "hidden", color: "var(--ink)", fontWeight: "bold" }}>
                    {calcInput}
                </div>
                <div style={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: "8px" }}>
                    <button type="button" className="btn-ghost" style={{ gridColumn: "span 3", backgroundColor: "#fee2e2", color: "#b91c1c", fontWeight: "bold", fontSize: "1.2rem" }} onClick={() => { setCalcInput("0"); setCalcPrevValue(null); setCalcOperator(null); setCalcNewNumber(false); }}>C</button>
                    <button type="button" className="btn-ghost" style={{ backgroundColor: "#e0e7ff", color: "#4338ca", fontWeight: "bold", fontSize: "1.2rem" }} onClick={() => handleCalcOp("/")}>÷</button>
                    <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("7")}>7</button>
                    <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("8")}>8</button>
                    <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("9")}>9</button>
                    <button type="button" className="btn-ghost" style={{ backgroundColor: "#e0e7ff", color: "#4338ca", fontWeight: "bold", fontSize: "1.2rem" }} onClick={() => handleCalcOp("*")}>×</button>
                    <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("4")}>4</button>
                    <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("5")}>5</button>
                    <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("6")}>6</button>
                    <button type="button" className="btn-ghost" style={{ backgroundColor: "#e0e7ff", color: "#4338ca", fontWeight: "bold", fontSize: "1.5rem" }} onClick={() => handleCalcOp("-")}>-</button>
                    <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("1")}>1</button>
                    <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("2")}>2</button>
                    <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("3")}>3</button>
                    <button type="button" className="btn-ghost" style={{ backgroundColor: "#e0e7ff", color: "#4338ca", fontWeight: "bold", fontSize: "1.2rem" }} onClick={() => handleCalcOp("+")}>+</button>
                    <button type="button" className="btn-ghost" style={{ gridColumn: "span 2", backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("0")}>0</button>
                    <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontWeight: "bold", fontSize: "1.2rem" }} onClick={() => handleCalcNum(".")}>.</button>
                    <button type="button" className="waiter-cta" style={{ margin: 0, fontSize: "1.2rem" }} onClick={handleCalcEqual}>=</button>
                </div>
            </div>
        </div>
    );
}