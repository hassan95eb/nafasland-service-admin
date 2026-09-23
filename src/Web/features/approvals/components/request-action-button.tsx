"use client";

import { useState, type FormEvent, type ReactNode } from "react";

import { useCreateApprovalRequest } from "@/features/approvals/hooks/use-approvals";
import { presentApiError } from "@/shared/lib/api-client";
import { Can } from "@/shared/permissions/permission-context";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";

/**
 * The single "درخواست …" action used for all four Approvals write paths
 * (delete product, publish/unpublish, featured/most, delete variant) — ADR-011:
 * the Admin never gets a direct action button, only a request form with a
 * required reason.
 */
export function RequestActionButton({
  permission,
  label,
  confirmLabel,
  requestType,
  targetEntityType,
  targetEntityId,
  payload,
  disabled = false,
  disabledReason,
}: {
  permission: string;
  label: string;
  confirmLabel?: string;
  requestType: string;
  targetEntityType: string;
  targetEntityId: string;
  payload: Record<string, unknown>;
  disabled?: boolean;
  disabledReason?: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string>();
  const mutation = useCreateApprovalRequest();

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(undefined);
    if (!reason.trim()) {
      setError("نوشتن دلیل درخواست الزامی است.");
      return;
    }

    try {
      await mutation.mutateAsync({ requestType, targetEntityType, targetEntityId, reason: reason.trim(), payload });
      setOpen(false);
      setReason("");
    } catch (requestError) {
      setError(presentApiError(requestError));
    }
  }

  return (
    <Can permission={permission}>
      <div className="space-y-2">
        <Button type="button" variant="secondary" disabled={disabled} onClick={() => setOpen((value) => !value)}>
          {open ? "انصراف" : label}
        </Button>
        {disabled && disabledReason ? <p className="text-xs text-[var(--muted)]">{disabledReason}</p> : null}

        {open ? (
          <form className="space-y-3 rounded-lg border border-[var(--border)] bg-neutral-50 p-4" onSubmit={handleSubmit} noValidate>
            {error ? <Alert>{error}</Alert> : null}
            <div className="space-y-2">
              <label className="block text-sm font-bold" htmlFor={`reason-${requestType}-${targetEntityId}`}>دلیل درخواست</label>
              <textarea
                id={`reason-${requestType}-${targetEntityId}`}
                className="min-h-24 w-full rounded-lg border border-[var(--border)] bg-white p-3 text-sm shadow-sm"
                value={reason}
                onChange={(event) => setReason(event.target.value)}
                required
              />
            </div>
            <Button type="submit" loading={mutation.isPending}>
              {mutation.isPending ? "در حال ثبت…" : confirmLabel ?? "ثبت درخواست"}
            </Button>
          </form>
        ) : null}
      </div>
    </Can>
  );
}
