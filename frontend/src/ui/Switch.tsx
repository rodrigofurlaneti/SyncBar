interface Props {
  checked: boolean;
  onChange: (next: boolean) => void;
  /** rótulo acessível obrigatório (role="switch") */
  label: string;
  disabled?: boolean;
}

/** Interruptor acessível: role="switch" + aria-checked, navegável por teclado. */
export function Switch({ checked, onChange, label, disabled = false }: Props) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      className={`switch ${checked ? "is-on" : ""}`}
      onClick={() => onChange(!checked)}
    >
      <span className="switch-knob" />
    </button>
  );
}
