import type { ReactNode } from "react";

export function Alert({ children, tone = "danger" }: { children: ReactNode; tone?: "danger" | "warning" }) {
  const colors =
    tone === "warning"
      ? "bg-[var(--warning-bg)] text-[var(--warning-fg)]"
      : "bg-red-50 text-[var(--danger)]";

  return <div className={`rounded-lg px-4 py-3 text-sm leading-7 ${colors}`}>{children}</div>;
}
