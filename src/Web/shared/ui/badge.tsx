import type { ReactNode } from "react";

export function Badge({ children, tone }: { children: ReactNode; tone: "success" | "warning" | "neutral" }) {
  const colors = {
    success: "bg-emerald-100 text-emerald-800",
    warning: "bg-amber-100 text-amber-800",
    neutral: "bg-neutral-100 text-neutral-700",
  }[tone];

  return <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-bold ${colors}`}>{children}</span>;
}
