import type { ButtonHTMLAttributes, ReactNode } from "react";

type Variant = "primary" | "ghost" | "danger";
type Size = "md" | "sm";

interface BaseProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, "className"> {
  variant?: Variant;
  size?: Size;
  loading?: boolean;
  block?: boolean;
  className?: string;
}

// iconOnly exige aria-label — garantido pelo tipo.
type IconProps =
  | { iconOnly: true; "aria-label": string; children: ReactNode }
  | { iconOnly?: false; children: ReactNode };

export type ButtonProps = BaseProps & IconProps;

const variantClass: Record<Variant, string> = {
  primary: "btn-primary",
  ghost: "btn-ghost",
  danger: "btn-danger",
};

export function Button({
  variant = "ghost",
  size = "md",
  loading = false,
  block = false,
  iconOnly = false,
  disabled,
  className = "",
  children,
  ...rest
}: ButtonProps) {
  const classes = [
    variantClass[variant],
    size === "sm" ? "btn-sm" : "",
    iconOnly ? "btn-icon" : "",
    block ? "btn-block" : "",
    loading ? "btn-loading" : "",
    className,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <button className={classes} disabled={disabled || loading} aria-busy={loading} {...rest}>
      {children}
    </button>
  );
}
