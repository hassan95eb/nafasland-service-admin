import { outcomeLabel } from "@/features/audit/lib/audit-labels";
import { Badge } from "@/shared/ui/badge";

const tones = { Success: "success", Failed: "danger", Denied: "warning" } as const;

export function AuditOutcomeBadge({ outcome }: { outcome: string }) {
  const tone = Object.hasOwn(tones, outcome) ? tones[outcome as keyof typeof tones] : "neutral";
  return <Badge tone={tone}>{outcomeLabel(outcome)}</Badge>;
}
