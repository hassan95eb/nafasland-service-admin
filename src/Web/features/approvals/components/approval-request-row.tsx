"use client";

import { useState, type FormEvent } from "react";

import {
  useApprovalRequest,
  useApproveApprovalRequest,
  useCancelApprovalRequest,
  useRejectApprovalRequest,
  useRetryApprovalRequest,
} from "@/features/approvals/hooks/use-approvals";
import type { ApprovalRequestSummary } from "@/features/approvals/api/approvals-types";
import { ApprovalStatusBadge, requestTypeLabel } from "@/features/approvals/components/approval-status-badge";
import { presentApiError } from "@/shared/lib/api-client";
import { formatPersianDateTime } from "@/shared/lib/formatters";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";

/**
 * One row in either cartable (SuperAdmin review or "my requests"). Expands to
 * fetch GET /approvals/{id}, which carries the live preview (ADR-010, rule 5) —
 * fetched on demand rather than for every row in the list.
 */
export function ApprovalRequestRow({ request, canReview }: { request: ApprovalRequestSummary; canReview: boolean }) {
  const [expanded, setExpanded] = useState(false);

  return (
    <article className="space-y-3 rounded-xl border border-[var(--border)] bg-white p-4 shadow-sm">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="space-y-1">
          <div className="flex flex-wrap items-center gap-2">
            <span className="font-bold">{requestTypeLabel(request.requestType)}</span>
            <ApprovalStatusBadge status={request.status} />
          </div>
          <p className="text-xs text-[var(--muted)]">
            هدف: {request.targetEntityType} #{request.targetEntityId} — ثبت‌شده در {formatPersianDateTime(request.requestedAt)}
          </p>
          <p className="text-sm">{request.reason}</p>
        </div>
        <Button type="button" variant="ghost" onClick={() => setExpanded((value) => !value)}>
          {expanded ? "بستن جزئیات" : "جزئیات"}
        </Button>
      </div>

      {expanded ? <ApprovalRequestDetailBody id={request.id} canReview={canReview} /> : null}
    </article>
  );
}

function ApprovalRequestDetailBody({ id, canReview }: { id: string; canReview: boolean }) {
  const query = useApprovalRequest(id);

  if (query.isPending) {
    return <p className="text-sm text-[var(--muted)]">در حال دریافت جزئیات…</p>;
  }
  if (query.error || !query.data) {
    return <Alert>{presentApiError(query.error)}</Alert>;
  }

  const request = query.data;
  return (
    <div className="space-y-4 border-t border-[var(--border)] pt-4">
      {request.preview ? (
        <div className="space-y-1">
          <p className="text-sm font-bold">{request.preview.entityTitle}</p>
          <dl className="grid grid-cols-2 gap-x-4 gap-y-1 text-xs text-[var(--muted)]">
            {request.preview.fields.map((field) => (
              <div key={field.label} className="contents">
                <dt className="font-bold">{field.label}</dt>
                <dd>{field.value ?? "—"}</dd>
              </div>
            ))}
          </dl>
        </div>
      ) : (
        <p className="text-xs text-[var(--muted)]">پیش‌نمایش زنده در دسترس نیست.</p>
      )}

      {request.reviewNote ? <p className="text-sm">یادداشت بازبین: {request.reviewNote}</p> : null}
      {request.executionError ? <Alert>خطای اجرا: {request.executionError}</Alert> : null}

      {canReview && request.status === "Pending" ? <ReviewActions id={request.id} /> : null}
      {canReview && request.status === "ExecutionFailed" ? <RetryAction id={request.id} /> : null}
      {!canReview && request.status === "Pending" ? <CancelAction id={request.id} /> : null}
    </div>
  );
}

function ReviewActions({ id }: { id: string }) {
  const approve = useApproveApprovalRequest();
  const reject = useRejectApprovalRequest();
  const [rejecting, setRejecting] = useState(false);
  const [note, setNote] = useState("");
  const [error, setError] = useState<string>();

  async function handleApprove() {
    setError(undefined);
    try {
      await approve.mutateAsync({ id });
    } catch (requestError) {
      setError(presentApiError(requestError));
    }
  }

  async function handleReject(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(undefined);
    if (!note.trim()) {
      setError("یادداشت رد الزامی است.");
      return;
    }
    try {
      await reject.mutateAsync({ id, note: note.trim() });
      setRejecting(false);
      setNote("");
    } catch (requestError) {
      setError(presentApiError(requestError));
    }
  }

  return (
    <div className="space-y-3">
      {error ? <Alert>{error}</Alert> : null}
      <div className="flex flex-wrap gap-2">
        <Button type="button" onClick={handleApprove} loading={approve.isPending}>
          تأیید و اجرا
        </Button>
        <Button type="button" variant="secondary" onClick={() => setRejecting((value) => !value)}>
          {rejecting ? "انصراف از رد" : "رد درخواست"}
        </Button>
      </div>
      {rejecting ? (
        <form className="space-y-2" onSubmit={handleReject} noValidate>
          <label className="block text-sm font-bold" htmlFor={`reject-note-${id}`}>یادداشت رد</label>
          <textarea
            id={`reject-note-${id}`}
            className="min-h-20 w-full rounded-lg border border-[var(--border)] bg-white p-3 text-sm shadow-sm"
            value={note}
            onChange={(event) => setNote(event.target.value)}
            required
          />
          <Button type="submit" variant="secondary" loading={reject.isPending}>
            ثبت رد
          </Button>
        </form>
      ) : null}
    </div>
  );
}

function RetryAction({ id }: { id: string }) {
  const retry = useRetryApprovalRequest();
  const [error, setError] = useState<string>();

  async function handleRetry() {
    setError(undefined);
    try {
      await retry.mutateAsync(id);
    } catch (requestError) {
      setError(presentApiError(requestError));
    }
  }

  return (
    <div className="space-y-2">
      {error ? <Alert>{error}</Alert> : null}
      <Button type="button" onClick={handleRetry} loading={retry.isPending}>
        تلاش دوباره برای اجرا
      </Button>
    </div>
  );
}

function CancelAction({ id }: { id: string }) {
  const cancel = useCancelApprovalRequest();
  const [error, setError] = useState<string>();

  async function handleCancel() {
    setError(undefined);
    try {
      await cancel.mutateAsync(id);
    } catch (requestError) {
      setError(presentApiError(requestError));
    }
  }

  return (
    <div className="space-y-2">
      {error ? <Alert>{error}</Alert> : null}
      <Button type="button" variant="secondary" onClick={handleCancel} loading={cancel.isPending}>
        لغو درخواست
      </Button>
    </div>
  );
}
