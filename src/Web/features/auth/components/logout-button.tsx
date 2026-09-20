"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { logout } from "@/features/auth/api/logout";
import { clearAntiforgeryToken, presentApiError } from "@/shared/lib/api-client";
import { Button } from "@/shared/ui/button";

export function LogoutButton() {
  const router = useRouter();
  const [error, setError] = useState<string>();
  const [isPending, setIsPending] = useState(false);

  async function handleLogout() {
    setError(undefined);
    setIsPending(true);
    try {
      await logout();
      clearAntiforgeryToken();
      router.replace("/login");
      router.refresh();
    } catch (requestError) {
      setError(presentApiError(requestError));
      setIsPending(false);
    }
  }

  return (
    <div className="space-y-2">
      <Button className="w-full" variant="secondary" onClick={handleLogout} disabled={isPending}>
        {isPending ? "در حال خروج…" : "خروج"}
      </Button>
      {error ? <p className="text-xs leading-5 text-red-700">{error}</p> : null}
    </div>
  );
}
