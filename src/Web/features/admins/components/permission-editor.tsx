"use client";

import type { AdminDetails, PermissionEffect } from "@/features/admins/api/admins";
import { usePermissions, useSetAdminPermission } from "@/features/admins/hooks/use-admins";
import { presentApiError } from "@/shared/lib/api-client";
import { Alert } from "@/shared/ui/alert";

export function PermissionEditor({ admin }: { admin: AdminDetails }) {
  const permissions = usePermissions();
  const mutation = useSetAdminPermission(admin.id);
  const isSuperAdmin = admin.roles.includes("SuperAdmin");

  async function change(key: string, effect: PermissionEffect) {
    try {
      await mutation.mutateAsync({ key, effect });
    } catch {
      // The mutation error is rendered below with ProblemDetails.
    }
  }

  return (
    <section className="space-y-4 rounded-xl border border-[var(--border)] bg-white p-5 shadow-sm">
      <div>
        <h2 className="text-lg font-black">دسترسی‌های مستقیم</h2>
        <p className="mt-1 text-sm text-[var(--muted)]">Deny بر permissionهای نقش اولویت دارد.</p>
      </div>
      {permissions.error ? <Alert>{presentApiError(permissions.error)}</Alert> : null}
      {mutation.error ? <Alert>{presentApiError(mutation.error)}</Alert> : null}
      <div className="space-y-3">
        {(permissions.data ?? []).map((permission) => {
          const current = admin.directPermissions.find((item) => item.key === permission.key)?.effect ?? "";
          const disabled = permission.isSuperAdminOnly && !isSuperAdmin;
          return (
            <label className="grid gap-2 rounded-lg bg-neutral-50 p-3 text-sm sm:grid-cols-[minmax(0,1fr)_12rem] sm:items-center" key={permission.key} title={disabled ? "این permission فقط برای کاربر SuperAdmin قابل Grant است." : undefined}>
              <span><strong>{permission.displayName}</strong><span className="mt-1 block text-xs text-[var(--muted)]">{permission.moduleName}</span></span>
              <select
                className="min-h-10 rounded-lg border border-[var(--border)] bg-white px-3"
                value={current}
                disabled={disabled || mutation.isPending}
                onChange={(event) => change(permission.key, (event.target.value || null) as PermissionEffect)}
              >
                <option value="">بدون override</option>
                <option value="Grant">Grant</option>
                <option value="Deny">Deny</option>
              </select>
            </label>
          );
        })}
      </div>
    </section>
  );
}
