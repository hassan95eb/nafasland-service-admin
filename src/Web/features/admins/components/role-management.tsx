"use client";

import { useState } from "react";

import type { PermissionDetails, RoleDetails } from "@/features/admins/api/admins";
import { usePermissions, useRoles, useSetRolePermissions } from "@/features/admins/hooks/use-admins";
import { presentApiError } from "@/shared/lib/api-client";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";

function RolePermissionCard({ role, permissions }: { role: RoleDetails; permissions: PermissionDetails[] }) {
  const mutation = useSetRolePermissions();
  const [selected, setSelected] = useState<string[]>(role.permissionKeys);
  const [message, setMessage] = useState<string>();

  async function save() {
    setMessage(undefined);
    try {
      await mutation.mutateAsync({ roleId: role.id, permissionKeys: selected });
      setMessage("دسترسی‌های نقش ذخیره شد.");
    } catch (error) {
      setMessage(presentApiError(error));
    }
  }

  return (
    <article className="space-y-4 rounded-xl border border-[var(--border)] p-4">
      <div>
        <h3 className="font-black">{role.name}</h3>
        {role.isSystemManaged ? <p className="mt-1 text-xs text-[var(--muted)]">این نقش سیستمی است و قابل ویرایش نیست.</p> : null}
      </div>
      <div className="grid gap-2 sm:grid-cols-2">
        {permissions.map((permission) => {
          const disabled = role.isSystemManaged || permission.isSuperAdminOnly;
          return (
            <label className="flex items-start gap-2 rounded-lg bg-neutral-50 p-3 text-sm" key={permission.key} title={permission.isSuperAdminOnly ? "این دسترسی فقط برای SuperAdmin است." : undefined}>
              <input
                className="mt-1 size-4 accent-[var(--primary)]"
                type="checkbox"
                checked={selected.includes(permission.key)}
                disabled={disabled}
                onChange={(event) => setSelected((current) => event.target.checked ? [...current, permission.key] : current.filter((key) => key !== permission.key))}
              />
              <span><strong>{permission.displayName}</strong><span className="mt-1 block text-xs text-[var(--muted)]">{permission.moduleName}</span></span>
            </label>
          );
        })}
      </div>
      {message ? <Alert tone={message.includes("ذخیره شد") ? "warning" : "danger"}>{message}</Alert> : null}
      <Button type="button" disabled={role.isSystemManaged || mutation.isPending} onClick={save}>ذخیرهٔ دسترسی‌های نقش</Button>
    </article>
  );
}

export function RoleManagement() {
  const roles = useRoles();
  const permissions = usePermissions();

  return (
    <section className="space-y-4 rounded-xl border border-[var(--border)] bg-white p-5 shadow-sm">
      <div>
        <h2 className="text-lg font-black">مدیریت نقش‌ها</h2>
        <p className="mt-1 text-sm text-[var(--muted)]">permissionهای نقش‌های غیرسیستمی را تعیین کنید.</p>
      </div>
      {roles.error ? <Alert>{presentApiError(roles.error)}</Alert> : null}
      {permissions.error ? <Alert>{presentApiError(permissions.error)}</Alert> : null}
      {roles.isPending || permissions.isPending ? (
        <p className="py-8 text-center text-sm text-[var(--muted)]">در حال دریافت نقش‌ها و دسترسی‌ها…</p>
      ) : (
        <div className="space-y-4">{(roles.data ?? []).map((role) => <RolePermissionCard key={role.id} role={role} permissions={permissions.data ?? []} />)}</div>
      )}
    </section>
  );
}
