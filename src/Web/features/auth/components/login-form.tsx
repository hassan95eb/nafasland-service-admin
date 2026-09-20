"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";

import { login } from "@/features/auth/api/login";
import { loginSchema } from "@/features/auth/schemas/login-schema";
import { presentApiError, storeAntiforgeryToken } from "@/shared/lib/api-client";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";

export function LoginForm() {
  const router = useRouter();
  const [error, setError] = useState<string>();
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(undefined);

    const formData = new FormData(event.currentTarget);
    const parsed = loginSchema.safeParse({
      username: formData.get("username"),
      password: formData.get("password"),
    });

    if (!parsed.success) {
      setError(parsed.error.issues[0]?.message ?? "اطلاعات ورود را بررسی کنید.");
      return;
    }

    setIsSubmitting(true);
    try {
      const result = await login(parsed.data);
      storeAntiforgeryToken(result.antiforgeryToken);
      router.replace(result.mustChangePassword ? "/change-password" : "/products");
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
        <label className="block text-sm font-bold" htmlFor="username">
          نام کاربری
        </label>
        <Input id="username" name="username" autoComplete="username" autoFocus />
      </div>

      <div className="space-y-2">
        <label className="block text-sm font-bold" htmlFor="password">
          رمز عبور
        </label>
        <Input id="password" name="password" type="password" autoComplete="current-password" />
      </div>

      <Button className="w-full" type="submit" disabled={isSubmitting}>
        {isSubmitting ? "در حال ورود…" : "ورود به پنل"}
      </Button>
    </form>
  );
}
