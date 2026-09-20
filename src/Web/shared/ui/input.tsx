import type { InputHTMLAttributes } from "react";

export function Input({ className = "", ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return (
    <input
      className={`min-h-11 w-full rounded-lg border border-[var(--border)] bg-white px-3 text-sm shadow-sm placeholder:text-neutral-400 ${className}`}
      {...props}
    />
  );
}
