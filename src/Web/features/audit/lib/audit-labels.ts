// Persian labels for AuditLog's raw English values. Every Action/EntityType
// written by steps 0–8 is listed here (from each IAuditableCommand's AuditAction,
// the Approvals coordinator and the Approvals/Auditing background jobs); one
// this map does not know yet is shown under its raw name, never as an error.

export const actionLabels: Readonly<Record<string, string>> = {
  ProductCreated: "ایجاد محصول",
  ProductUpdated: "ویرایش محصول",
  VariantPriceInventoryUpdated: "تغییر قیمت و موجودی واریانت",
  ApprovalRequested: "ثبت درخواست تأیید",
  ApprovalApproved: "تأیید درخواست",
  ApprovalRejected: "رد درخواست",
  ApprovalCancelled: "لغو درخواست",
  ApprovalExecuted: "اجرای درخواست تأییدشده",
  ApprovalExecutionFailed: "شکست در اجرای درخواست",
  ApprovalExpired: "انقضای درخواست",
  UserCreated: "ایجاد کاربر",
  UserPasswordReset: "بازنشانی رمز کاربر",
  UserRolesChanged: "تغییر نقش‌های کاربر",
  UserPermissionChanged: "تغییر دسترسی مستقیم کاربر",
  UserActiveStatusChanged: "فعال یا غیرفعال‌کردن کاربر",
  RolePermissionsChanged: "تغییر دسترسی‌های نقش",
  AuditExported: "خروجی گرفتن از گزارش فعالیت",
  AuditPurged: "پاک‌سازی دوره‌ای گزارش فعالیت",
  PingSent: "پینگ آزمایشی",
};

export const entityTypeLabels: Readonly<Record<string, string>> = {
  Product: "محصول",
  ProductVariant: "واریانت",
  // What the product page files variant-delete approval requests under.
  Variant: "واریانت (درخواست تأیید)",
  User: "کاربر",
  Role: "نقش",
  AuditLog: "گزارش فعالیت",
  PingRecord: "پینگ آزمایشی",
};

export const outcomeLabels = {
  Success: "موفق",
  Failed: "ناموفق",
  Denied: "ردشده (بدون دسترسی)",
} as const;

export type AuditOutcomeName = keyof typeof outcomeLabels;

export const outcomeNames = Object.keys(outcomeLabels) as AuditOutcomeName[];

export function isAuditOutcome(value: string | null | undefined): value is AuditOutcomeName {
  return value != null && Object.hasOwn(outcomeLabels, value);
}

export function actionLabel(action: string) {
  return Object.hasOwn(actionLabels, action) ? actionLabels[action] : action;
}

export function entityTypeLabel(entityType: string | null | undefined) {
  if (!entityType) {
    return "—";
  }
  return Object.hasOwn(entityTypeLabels, entityType) ? entityTypeLabels[entityType] : entityType;
}

export function outcomeLabel(outcome: string) {
  return isAuditOutcome(outcome) ? outcomeLabels[outcome] : outcome;
}

/** ADR-014's activity cards. Unlisted Actions fall into "other", so a new module never breaks the page. */
export const activityCategories = [
  { key: "create", label: "ایجاد", actions: ["ProductCreated", "UserCreated"] },
  {
    key: "edit",
    label: "ویرایش",
    actions: [
      "ProductUpdated",
      "VariantPriceInventoryUpdated",
      "UserPasswordReset",
      "UserRolesChanged",
      "UserPermissionChanged",
      "UserActiveStatusChanged",
      "RolePermissionsChanged",
    ],
  },
  { key: "request", label: "درخواست تأیید (حذف، انتشار، وضعیت)", actions: ["ApprovalRequested"] },
  {
    key: "decision",
    label: "تصمیم روی درخواست‌ها",
    actions: ["ApprovalApproved", "ApprovalRejected", "ApprovalCancelled", "ApprovalExecuted", "ApprovalExecutionFailed"],
  },
] as const;

export interface ActionCount {
  action: string;
  outcome: string;
  count: number | string;
}

export interface ActivitySummary {
  categories: { key: string; label: string; count: number }[];
  other: number;
  failed: number;
  denied: number;
}

/**
 * Category cards count successful actions only; a failed or denied attempt is
 * counted once, in its own card, not also as a "create" or "edit".
 */
export function summarizeActivity(counts: readonly ActionCount[]): ActivitySummary {
  const categoryOf = new Map<string, string>();
  for (const category of activityCategories) {
    for (const action of category.actions) {
      categoryOf.set(action, category.key);
    }
  }

  const totals = new Map<string, number>(activityCategories.map((category) => [category.key, 0]));
  let other = 0;
  let failed = 0;
  let denied = 0;

  for (const item of counts) {
    const count = Number(item.count);
    if (item.outcome === "Denied") {
      denied += count;
    } else if (item.outcome === "Failed") {
      failed += count;
    } else {
      const key = categoryOf.get(item.action);
      if (key) {
        totals.set(key, (totals.get(key) ?? 0) + count);
      } else {
        other += count;
      }
    }
  }

  return {
    categories: activityCategories.map((category) => ({
      key: category.key,
      label: category.label,
      count: totals.get(category.key) ?? 0,
    })),
    other,
    failed,
    denied,
  };
}

export interface ActorLike {
  actorUserId: string | null;
  actorUsername: string | null;
}

/** "سیستم" for a record with no actor (a background job), "کاربر ناشناس" + short id for a user Identity no longer knows. */
export function actorDisplayName(actor: ActorLike) {
  if (!actor.actorUserId) {
    return "سیستم";
  }
  return actor.actorUsername ?? `کاربر ناشناس (${actor.actorUserId.slice(0, 8)})`;
}
