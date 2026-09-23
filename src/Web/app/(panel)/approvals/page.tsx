import { MyApprovalRequests } from "@/features/approvals/components/my-approval-requests";
import { requireAuthenticatedUser } from "@/shared/permissions/require-permission";

export default async function MyApprovalsPage() {
  await requireAuthenticatedUser();
  return <MyApprovalRequests />;
}
