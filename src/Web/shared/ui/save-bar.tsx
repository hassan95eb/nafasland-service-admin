import type { ReactNode } from "react";

import { Button } from "@/shared/ui/button";

export function SaveBar({
  message,
  saveLabel = "ذخیره",
  loading = false,
  disabled = false,
}: {
  message?: ReactNode;
  saveLabel?: string;
  loading?: boolean;
  disabled?: boolean;
}) {
  return (
    <div className="fixed inset-x-0 bottom-0 z-20 border-t border-[var(--border)] bg-white/95 backdrop-blur supports-[backdrop-filter]:bg-white/80 lg:right-64">
      <div className="mx-auto flex w-full max-w-5xl flex-wrap items-center justify-between gap-3 px-4 py-3 sm:px-6 lg:px-8">
        <div className="min-w-0 flex-1 truncate text-sm font-bold text-[var(--danger)]">{message}</div>
        <Button type="submit" loading={loading} disabled={disabled}>
          {saveLabel}
        </Button>
      </div>
    </div>
  );
}
