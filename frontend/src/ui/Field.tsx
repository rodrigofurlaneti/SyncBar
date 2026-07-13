import { useId } from "react";
import type {
  InputHTMLAttributes,
  ReactNode,
  SelectHTMLAttributes,
} from "react";

interface FieldWrapProps {
  label: ReactNode;
  hint?: ReactNode;
  error?: ReactNode;
  children: (props: { id: string; "aria-invalid"?: boolean; "aria-describedby"?: string }) => ReactNode;
}

/** Rótulo persistente + erro/hint acessíveis (htmlFor/aria-describedby). */
export function Field({ label, hint, error, children }: FieldWrapProps) {
  const id = useId();
  const describedBy = error ? `${id}-err` : hint ? `${id}-hint` : undefined;
  return (
    <div className="field">
      <label className="field-label" htmlFor={id}>
        {label}
      </label>
      {children({ id, "aria-invalid": error ? true : undefined, "aria-describedby": describedBy })}
      {hint && !error && (
        <span className="field-hint" id={`${id}-hint`}>
          {hint}
        </span>
      )}
      {error && (
        <span className="field-error" id={`${id}-err`}>
          {error}
        </span>
      )}
    </div>
  );
}

/** Input com rótulo — atalho para o caso comum. */
export function TextField({
  label,
  hint,
  error,
  ...rest
}: { label: ReactNode; hint?: ReactNode; error?: ReactNode } & InputHTMLAttributes<HTMLInputElement>) {
  return (
    <Field label={label} hint={hint} error={error}>
      {(a11y) => <input {...a11y} {...rest} />}
    </Field>
  );
}

/** Select com rótulo. */
export function SelectField({
  label,
  hint,
  error,
  children,
  ...rest
}: {
  label: ReactNode;
  hint?: ReactNode;
  error?: ReactNode;
} & SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <Field label={label} hint={hint} error={error}>
      {(a11y) => (
        <select {...a11y} {...rest}>
          {children}
        </select>
      )}
    </Field>
  );
}
