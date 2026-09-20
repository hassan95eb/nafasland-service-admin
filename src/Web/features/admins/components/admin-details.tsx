"use client";

import Link from "next/link";

import { PermissionEditor } from "@/features/admins/components/permission-editor";
import { RoleEditor } from "@/features/admins/components/role-editor";
import { useAdmin } from "@/features/admins/hooks/use-admins";
import { presentApiError } from "@/shared/lib/api-client";
import { formatPersianDate } from "@/shared/lib/formatters";
import { Alert } from "@/shared/ui/alert";
import { Badge } from "@/shared/ui/badge";

export function AdminDetailsView({ id, canManageAccess }: { id: string; canManageAccess: boolean }) {
  const query = useAdmin(id);

  if (query.isPending) {
    return <p className="py-12 text-center text-sm text-[var(--muted)]">در حال دریافت مشخصات ادمین…</p>;
  }
  if (query.error || !query.data) {
    return <Alert>{presentApiError(query.error)}</Alert>;
  }
  const admin = query.data;

  return (
    <section className="space-y-6">
      <Link className="text-sm font-bold text-[var(--primary)]" href="/admins">بازگشت به فهرست ادمین‌ها</Link>
      <header className="rounded-xl border border-[var(--border)] bg-white p-5 shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div><h1 className="text-2xl font-black">{admin.username}</h1><p className="mt-1 text-sm text-[var(--muted)]">ساخته‌شده در {formatPersianDate(admin.createdAt)}</p></div>
          <div className="flex gap-2"><Badge tone={admin.isActive ? "success" : "neutral"}>{admin.isActive ? "فعال" : "غیرفعال"}</Badge><Badge tone={admin.mustChangePassword ? "warning" : "success"}>{admin.mustChangePassword ? "در انتظار تغییر رمز" : "رمز تغییر کرده"}</Badge></div>
        </div>
      </header>
      <RoleEditor admin={admin} canManageAccess={canManageAccess} />
      {canManageAccess ? <PermissionEditor admin={admin} /> : (
        <Alert tone="warning">برای مشاهده و تغییر permissionهای مستقیم، دسترسی مدیریت دسترسی‌ها لازم است.</Alert>
      )}
    </section>
  );
}
