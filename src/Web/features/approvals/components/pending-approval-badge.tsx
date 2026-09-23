"use client";

import { usePendingApprovalForTarget } from "@/features/approvals/hooks/use-approvals";
import { Badge } from "@/shared/ui/badge";

export function PendingApprovalBadge({ targetEntityType, targetEntityId }: { targetEntityType: string; targetEntityId: string | null }) {
  const query = usePendingApprovalForTarget(targetEntityType, targetEntityId);
  if (!query.data || query.data.items.length === 0) {
    return null;
  }

  return <Badge tone="warning">در انتظار تأیید سوپرادمین</Badge>;
}
