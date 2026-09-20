"use client";

import { useState, type FormEvent } from "react";

import { GeneratedPasswordReveal } from "@/features/admins/components/generated-password-reveal";
import { useCreateAdmin } from "@/features/admins/hooks/use-admins";
import { generateTemporaryPassword } from "@/features/admins/lib/generate-temp-password";
import { createAdminSchema } from "@/features/admins/schemas/admin-schema";
import { presentApiError } from "@/shared/lib/api-client";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";

export function CreateAdminForm() {
  const mutation = useCreateAdmin();
  const [error, setError] = useState<string>();
  const [revealed, setRevealed] = useState<{ username: string; password: string }>();

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(undefined);
    setRevealed(undefined);
    const form = event.currentTarget;
    const parsed = createAdminSchema.safeParse({ username: new FormData(form).get("username") });
    if (!parsed.success) {
      setError(parsed.error.issues[0]?.message ?? "نام کاربری را بررسی کنید.");
      return;
    }

    const password = generateTemporaryPassword();
    try {
      await mutation.mutateAsync({ username: parsed.data.username, initialPassword: password });
      setRevealed({ username: parsed.data.username, password });
      form.reset();
    } catch (requestError) {
      setError(presentApiError(requestError));
    }
  }

  return (
    <section className="space-y-4 rounded-xl border border-[var(--border)] bg-white p-5 shadow-sm">
      <div>
        <h2 className="text-lg font-black">ساخت ادمین تازه</h2>
        <p className="mt-1 text-sm text-[var(--muted)]">رمز موقت امن در همین مرورگر ساخته می‌شود.</p>
      </div>
      {error ? <Alert>{error}</Alert> : null}
      {revealed ? <GeneratedPasswordReveal {...revealed} /> : null}
      <form className="flex flex-col gap-3 sm:flex-row sm:items-end" onSubmit={handleSubmit} noValidate>
        <div className="flex-1 space-y-2">
          <label className="block text-sm font-bold" htmlFor="new-admin-username">نام کاربری</label>
          <Input id="new-admin-username" name="username" autoComplete="off" />
        </div>
        <Button type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? "در حال ساخت…" : "ساخت ادمین"}
        </Button>
      </form>
    </section>
  );
}
