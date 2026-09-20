"use client";

import { useState } from "react";

import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";

export function GeneratedPasswordReveal({ username, password }: { username: string; password: string }) {
  const [copied, setCopied] = useState(false);

  async function copyPassword() {
    await navigator.clipboard.writeText(password);
    setCopied(true);
  }

  return (
    <Alert tone="warning">
      <p className="font-bold">رمز موقت {username} فقط همین یک‌بار نمایش داده می‌شود.</p>
      <p className="mt-1">همین حالا آن را به ادمین تحویل دهید؛ بعداً قابل بازیابی نیست.</p>
      <div className="mt-3 flex flex-wrap items-center gap-3" dir="ltr">
        <code className="rounded bg-white/70 px-3 py-2 text-base font-bold tracking-wider">{password}</code>
        <Button type="button" variant="secondary" onClick={copyPassword}>
          {copied ? "کپی شد" : "کپی رمز"}
        </Button>
      </div>
    </Alert>
  );
}
