"use client";

import { AdminsTable } from "@/features/admins/components/admins-table";
import { CreateAdminForm } from "@/features/admins/components/create-admin-form";
import { RoleManagement } from "@/features/admins/components/role-management";
import { useAdmins } from "@/features/admins/hooks/use-admins";
import { presentApiError } from "@/shared/lib/api-client";
import { Alert } from "@/shared/ui/alert";

export function AdminManagement({ canManageAccess }: { canManageAccess: boolean }) {
  const query = useAdmins();

  return (
    <section className="space-y-6">
      <header>
        <h1 className="text-2xl font-black">مدیریت ادمین‌ها</h1>
        <p className="mt-1 text-sm text-[var(--muted)]">ساخت حساب، وضعیت، رمز و دسترسی‌های کارکنان پنل</p>
      </header>
      <CreateAdminForm />
      {query.error ? <Alert>{presentApiError(query.error)}</Alert> : null}
      {query.isPending ? <p className="py-10 text-center text-sm text-[var(--muted)]">در حال دریافت ادمین‌ها…</p> : <AdminsTable admins={query.data ?? []} />}
      {canManageAccess ? <RoleManagement /> : null}
    </section>
  );
}
