"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";

import { changePassword } from "@/features/auth/api/change-password";
import { changePasswordSchema } from "@/features/auth/schemas/change-password-schema";
import { presentApiError } from "@/shared/lib/api-client";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";

export function ChangePasswordForm({ returnTo = "/products" }: { returnTo?: string }) {
  const router = useRouter();
  const [error, setError] = useState<string>();
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(undefined);
    const formData = new FormData(event.currentTarget);
    const parsed = changePasswordSchema.safeParse({
      currentPassword: formData.get("currentPassword"),
      newPassword: formData.get("newPassword"),
    });
    if (!parsed.success) {
      setError(parsed.error.issues[0]?.message ?? "رمزها را بررسی کنید.");
      return;
    }

    setIsSubmitting(true);
    try {
      await changePassword(parsed.data);
      router.replace(returnTo.startsWith("/") && !returnTo.startsWith("//") ? returnTo : "/products");
      router.refresh();
    } catch (requestError) {
      setError(presentApiError(requestError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form className="space-y-5" onSubmit={handleSubmit} noValidate>
      {error ? <Alert>{error}</Alert> : null}
      <div className="space-y-2">
        <label className="block text-sm font-bold" htmlFor="current-password">رمز عبور فعلی</label>
        <Input id="current-password" name="currentPassword" type="password" autoComplete="current-password" autoFocus />
      </div>
      <div className="space-y-2">
        <label className="block text-sm font-bold" htmlFor="new-password">رمز عبور جدید</label>
        <Input id="new-password" name="newPassword" type="password" autoComplete="new-password" />
        <p className="text-xs text-[var(--muted)]">حداقل ۸ نویسه و متفاوت از رمز فعلی</p>
      </div>
      <Button className="w-full" type="submit" disabled={isSubmitting}>{isSubmitting ? "در حال تغییر…" : "تغییر رمز و ادامه"}</Button>
    </form>
  );
}
