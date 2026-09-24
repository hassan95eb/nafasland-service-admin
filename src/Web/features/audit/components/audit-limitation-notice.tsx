import { Alert } from "@/shared/ui/alert";

/** ADR-014's explicit limitation and ADR-015's retention window — on every report page, not dismissible. */
export function AuditLimitationNotice() {
  return (
    <div className="space-y-2">
      <Alert tone="warning">
        این گزارش فقط کارهایی را نشان می‌دهد که <strong>از همین پنل</strong> انجام شده‌اند. تا وقتی دسترسی به پنل خود
        نفس‌لند باز است، ممکن است تغییری مستقیم از آنجا انجام شده باشد؛ پس نبودن یک رویداد در این گزارش به‌معنای «انجام
        نشده» نیست.
      </Alert>
      <p className="text-xs leading-6 text-[var(--muted)]">
        رکوردهای قدیمی‌تر از ۶ ماه به‌طور خودکار بایگانی و از این نما حذف می‌شوند.
      </p>
    </div>
  );
}
