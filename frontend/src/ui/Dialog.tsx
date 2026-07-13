import { createContext, useCallback, useContext, useMemo, useRef, useState } from "react";
import type { ReactNode } from "react";
import { Modal } from "./Modal";
import { Button } from "./Button";

interface ConfirmOptions {
  title?: string;
  message: ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;
}

interface PromptOptions {
  title?: string;
  message?: ReactNode;
  label: string;
  defaultValue?: string;
  placeholder?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  inputMode?: "text" | "decimal" | "numeric";
}

interface DialogApi {
  confirm: (options: ConfirmOptions) => Promise<boolean>;
  prompt: (options: PromptOptions) => Promise<string | null>;
}

const DialogContext = createContext<DialogApi | null>(null);

type State =
  | { kind: "none" }
  | { kind: "confirm"; options: ConfirmOptions; resolve: (v: boolean) => void }
  | { kind: "prompt"; options: PromptOptions; resolve: (v: string | null) => void };

export function DialogProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<State>({ kind: "none" });
  const [value, setValue] = useState("");
  const stateRef = useRef(state);
  stateRef.current = state;

  const close = useCallback(() => setState({ kind: "none" }), []);

  const confirm = useCallback(
    (options: ConfirmOptions) =>
      new Promise<boolean>((resolve) => setState({ kind: "confirm", options, resolve })),
    [],
  );

  const prompt = useCallback(
    (options: PromptOptions) =>
      new Promise<string | null>((resolve) => {
        setValue(options.defaultValue ?? "");
        setState({ kind: "prompt", options, resolve });
      }),
    [],
  );

  const api = useMemo<DialogApi>(() => ({ confirm, prompt }), [confirm, prompt]);

  const settle = (result: boolean | string | null) => {
    const s = stateRef.current;
    if (s.kind === "confirm") s.resolve(result as boolean);
    if (s.kind === "prompt") s.resolve(result as string | null);
    close();
  };

  return (
    <DialogContext.Provider value={api}>
      {children}

      {state.kind === "confirm" && (
        <Modal title={state.options.title ?? "Confirmar"} onClose={() => settle(false)} ariaLabel="Confirmação">
          <p style={{ margin: 0, color: "var(--ink-dim)" }}>{state.options.message}</p>
          <div className="ui-row" style={{ justifyContent: "flex-end", marginTop: 4 }}>
            <Button onClick={() => settle(false)}>{state.options.cancelLabel ?? "Cancelar"}</Button>
            <Button
              variant={state.options.danger ? "danger" : "primary"}
              onClick={() => settle(true)}
            >
              {state.options.confirmLabel ?? "Confirmar"}
            </Button>
          </div>
        </Modal>
      )}

      {state.kind === "prompt" && (
        <Modal title={state.options.title ?? "Informe um valor"} onClose={() => settle(null)} ariaLabel="Entrada">
          <form
            className="ui-stack"
            onSubmit={(e) => {
              e.preventDefault();
              settle(value); // Confirmar devolve o texto (mesmo vazio); Cancelar devolve null.
            }}
          >
            {state.options.message && (
              <p style={{ margin: 0, color: "var(--ink-dim)" }}>{state.options.message}</p>
            )}
            <label className="field">
              <span className="field-label">{state.options.label}</span>
              <input
                autoFocus
                inputMode={state.options.inputMode ?? "text"}
                placeholder={state.options.placeholder}
                value={value}
                onChange={(e) => setValue(e.target.value)}
              />
            </label>
            <div className="ui-row" style={{ justifyContent: "flex-end" }}>
              <Button type="button" onClick={() => settle(null)}>
                {state.options.cancelLabel ?? "Cancelar"}
              </Button>
              <Button type="submit" variant="primary">
                {state.options.confirmLabel ?? "Confirmar"}
              </Button>
            </div>
          </form>
        </Modal>
      )}
    </DialogContext.Provider>
  );
}

export function useDialog(): DialogApi {
  const ctx = useContext(DialogContext);
  if (!ctx) throw new Error("useDialog precisa estar dentro de <DialogProvider>.");
  return ctx;
}
