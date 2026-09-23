import { ApprovalsCartable } from "@/features/approvals/components/approvals-cartable";
import { requirePermission } from "@/shared/permissions/require-permission";

export default async function ApprovalsReviewPage() {
  await requirePermission("approvals.read.all");
  return <ApprovalsCartable />;
}
