"use client";

import Link from "next/link";
import { useState } from "react";

import type { AdminSummary } from "@/features/admins/api/admins";
import { GeneratedPasswordReveal } from "@/features/admins/components/generated-password-reveal";
import { useResetAdminPassword, useToggleAdminActive } from "@/features/admins/hooks/use-admins";
import { generateTemporaryPassword } from "@/features/admins/lib/generate-temp-password";
import { presentApiError } from "@/shared/lib/api-client";
import { Alert } from "@/shared/ui/alert";
import { Badge } from "@/shared/ui/badge";
import { Button } from "@/shared/ui/button";

export function AdminsTable({ admins }: { admins: AdminSummary[] }) {
  const toggleMutation = useToggleAdminActive();
  const resetMutation = useResetAdminPassword();
  const [error, setError] = useState<string>();
  const [revealed, setRevealed] = useState<{ username: string; password: string }>();

  async function toggle(admin: AdminSummary) {
    setError(undefined);
    try {
      await toggleMutation.mutateAsync(admin.id);
    } catch (requestError) {
      setError(presentApiError(requestError));
    }
  }

  async function resetPassword(admin: AdminSummary) {
    setError(undefined);
    setRevealed(undefined);
    const password = generateTemporaryPassword();
    try {
      await resetMutation.mutateAsync({ id: admin.id, password });
      setRevealed({ username: admin.username, password });
    } catch (requestError) {
      setError(presentApiError(requestError));
    }
  }

  if (admins.length === 0) {
    return <p className="px-4 py-12 text-center text-sm text-[var(--muted)]">هنوز ادمینی ساخته نشده است.</p>;
  }

  return (
    <div className="space-y-4">
      {error ? <Alert>{error}</Alert> : null}
      {revealed ? <GeneratedPasswordReveal {...revealed} /> : null}
      <div className="overflow-x-auto rounded-xl border border-[var(--border)] bg-white shadow-sm">
        <table className="w-full min-w-[840px] border-collapse text-right text-sm">
          <thead className="bg-neutral-50 text-xs text-[var(--muted)]">
            <tr>
              <th className="px-4 py-3">نام کاربری</th>
              <th className="px-4 py-3">نقش‌ها</th>
              <th className="px-4 py-3">وضعیت</th>
              <th className="px-4 py-3">رمز اجباری</th>
              <th className="px-4 py-3">عملیات</th>
            </tr>
          </thead>
          <tbody>
            {admins.map((admin) => {
              const protectedReason = admin.isProtected ? "حساب seed‌شده محافظت‌شده است." : undefined;
              return (
                <tr className="border-t border-[var(--border)]" key={admin.id}>
                  <td className="px-4 py-4 font-bold">
                    <Link className="text-[var(--primary)] hover:underline" href={`/admins/${admin.id}`}>
                      {admin.username}
                    </Link>
                    {admin.isProtected ? <p className="mt-1 text-xs text-[var(--muted)]">حساب محافظت‌شده</p> : null}
                  </td>
                  <td className="px-4 py-4">{admin.roles.length ? admin.roles.join("، ") : "بدون نقش"}</td>
                  <td className="px-4 py-4"><Badge tone={admin.isActive ? "success" : "neutral"}>{admin.isActive ? "فعال" : "غیرفعال"}</Badge></td>
                  <td className="px-4 py-4"><Badge tone={admin.mustChangePassword ? "warning" : "success"}>{admin.mustChangePassword ? "باز" : "انجام‌شده"}</Badge></td>
                  <td className="px-4 py-4">
                    <div className="flex flex-wrap gap-2" title={protectedReason}>
                      <Button type="button" variant="secondary" disabled={admin.isProtected || toggleMutation.isPending} onClick={() => toggle(admin)}>
                        {admin.isActive ? "غیرفعال‌سازی" : "فعال‌سازی"}
                      </Button>
                      <Button type="button" variant="secondary" disabled={admin.isProtected || resetMutation.isPending} onClick={() => resetPassword(admin)}>
                        ریست رمز
                      </Button>
                      <Link className="inline-flex min-h-10 items-center rounded-lg px-3 font-bold text-[var(--primary)]" href={`/admins/${admin.id}`}>
                        جزئیات
                      </Link>
                    </div>
                    {protectedReason ? <p className="mt-2 text-xs text-[var(--muted)]">{protectedReason}</p> : null}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
