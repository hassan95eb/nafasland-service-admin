import type { ButtonHTMLAttributes } from "react";

import { Spinner } from "@/shared/ui/spinner";

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: "primary" | "secondary" | "ghost";
  loading?: boolean;
};

const variants = {
  primary: "bg-[var(--primary)] text-white hover:bg-[var(--primary-strong)]",
  secondary: "border border-[var(--border)] bg-white text-[var(--foreground)] hover:bg-neutral-50",
  ghost: "bg-transparent text-[var(--muted)] hover:bg-black/5",
};

export function Button({ className = "", variant = "primary", loading = false, disabled, children, ...props }: ButtonProps) {
  return (
    <button
      className={`inline-flex min-h-10 items-center justify-center gap-2 rounded-lg px-4 py-2 text-sm font-bold transition disabled:cursor-not-allowed disabled:opacity-50 ${variants[variant]} ${className}`}
      disabled={disabled || loading}
      aria-busy={loading}
      {...props}
    >
      {loading ? <Spinner /> : null}
      {children}
    </button>
  );
}
