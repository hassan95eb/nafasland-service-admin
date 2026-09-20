"use client";

import { useMemo, useState } from "react";

import type { AdminDetails } from "@/features/admins/api/admins";
import { useRoleSummaries, useSetAdminRoles } from "@/features/admins/hooks/use-admins";
import { presentApiError } from "@/shared/lib/api-client";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";

export function RoleEditor({ admin, canManageAccess }: { admin: AdminDetails; canManageAccess: boolean }) {
  const roles = useRoleSummaries();
  const mutation = useSetAdminRoles(admin.id);
  const roleIdsForNames = useMemo(
    () => (roles.data ?? []).filter((role) => admin.roles.includes(role.name)).map((role) => role.id),
    [admin.roles, roles.data],
  );
  const [selected, setSelected] = useState<string[] | null>(null);
  const [message, setMessage] = useState<string>();
  const effectiveSelected = selected ?? roleIdsForNames;

  async function save() {
    setMessage(undefined);
    try {
      await mutation.mutateAsync(effectiveSelected);
      setMessage("نقش‌های کاربر ذخیره شد.");
    } catch (error) {
      setMessage(presentApiError(error));
    }
  }

  return (
    <section className="space-y-4 rounded-xl border border-[var(--border)] bg-white p-5 shadow-sm">
      <div>
        <h2 className="text-lg font-black">نقش‌ها</h2>
        {admin.isProtected ? <p className="mt-1 text-sm text-[var(--muted)]">نقش حساب محافظت‌شده قابل تغییر نیست.</p> : null}
      </div>
      {roles.error ? <Alert>{presentApiError(roles.error)}</Alert> : null}
      <div className="flex flex-wrap gap-3">
        {(roles.data ?? []).map((role) => {
          const disabled = admin.isProtected || (role.isSystemManaged && !canManageAccess);
          const reason = admin.isProtected
            ? "حساب seed‌شده محافظت‌شده است."
            : role.isSystemManaged && !canManageAccess
              ? "انتساب SuperAdmin به دسترسی مدیریت دسترسی‌ها نیاز دارد."
              : undefined;
          return (
            <label className="flex items-center gap-2 rounded-lg border border-[var(--border)] px-3 py-2 text-sm" key={role.id} title={reason}>
              <input
                className="size-4 accent-[var(--primary)]"
                type="checkbox"
                checked={effectiveSelected.includes(role.id)}
                disabled={disabled}
                onChange={(event) => setSelected(event.target.checked ? [...effectiveSelected, role.id] : effectiveSelected.filter((id) => id !== role.id))}
              />
              {role.name}
            </label>
          );
        })}
      </div>
      {message ? <Alert tone={message.includes("ذخیره شد") ? "warning" : "danger"}>{message}</Alert> : null}
      <Button type="button" disabled={admin.isProtected || roles.isPending || mutation.isPending} onClick={save}>ذخیرهٔ نقش‌ها</Button>
    </section>
  );
}
