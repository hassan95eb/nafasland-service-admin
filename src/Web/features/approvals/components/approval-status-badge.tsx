import type { ApprovalRequestStatus } from "@/features/approvals/api/approvals-types";
import { Badge } from "@/shared/ui/badge";

const labels: Record<ApprovalRequestStatus, string> = {
  Pending: "در انتظار",
  Approved: "تأییدشده",
  Rejected: "ردشده",
  Executed: "اجراشده",
  ExecutionFailed: "اجرا ناموفق",
  Cancelled: "لغوشده",
  Expired: "منقضی‌شده",
};

const tones: Record<ApprovalRequestStatus, "success" | "warning" | "neutral"> = {
  Pending: "warning",
  Approved: "neutral",
  Rejected: "neutral",
  Executed: "success",
  ExecutionFailed: "neutral",
  Cancelled: "neutral",
  Expired: "neutral",
};

export function ApprovalStatusBadge({ status }: { status: ApprovalRequestStatus }) {
  return <Badge tone={tones[status]}>{labels[status]}</Badge>;
}

const requestTypeLabels: Record<string, string> = {
  "catalog.product.delete": "حذف محصول",
  "catalog.product.publish": "انتشار/لغو انتشار محصول",
  "catalog.product.status": "تغییر ویژه/پرفروش‌ترین",
  "catalog.variant.delete": "حذف واریانت",
  "returns.register": "ثبت مرجوعی",
};

export function requestTypeLabel(requestType: string) {
  return requestTypeLabels[requestType] ?? requestType;
}
