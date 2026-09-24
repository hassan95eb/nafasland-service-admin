"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";

import { usePendingApprovalForTarget, useCreateApprovalRequest } from "@/features/approvals/hooks/use-approvals";
import { returnRequestType, returnTargetEntityType, type OrderPreview } from "@/features/returns/api/returns-api";
import { OrderSummary } from "@/features/returns/components/order-summary";
import { useOrderPreview } from "@/features/returns/hooks/use-returns";
import { orderIdSchema, returnReasonMaxLength, returnRequestSchema } from "@/features/returns/schemas/return-request-schema";
import { presentApiError } from "@/shared/lib/api-client";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";

/**
 * ADR-054: fetch the order by its number, then file a returns.register
 * approval request with a reason and a return date. Only { orderId, reason,
 * returnDate } is sent; the server re-reads the order itself.
 */
export function ReturnRequestForm() {
  const [orderInput, setOrderInput] = useState("");
  const [orderInputError, setOrderInputError] = useState<string>();
  const [orderId, setOrderId] = useState<string | null>(null);
  const order = useOrderPreview(orderId);

  function handleFetchOrder(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const parsed = orderIdSchema.safeParse(orderInput);
    if (!parsed.success) {
      setOrderInputError(parsed.error.issues[0]?.message);
      return;
    }

    setOrderInputError(undefined);
    if (parsed.data === orderId) {
      void order.refetch();
    } else {
      setOrderId(parsed.data);
    }
  }

  return (
    <section className="space-y-5">
      <header className="space-y-1">
        <h1 className="text-2xl font-black">ثبت مرجوعی</h1>
        <p className="text-sm text-[var(--muted)]">سفارش از پرتال خوانده می‌شود؛ مرجوعی پس از تأیید سوپرادمین ثبت می‌شود.</p>
      </header>

      <form className="flex flex-wrap items-end gap-3 rounded-xl border border-[var(--border)] bg-white p-4 shadow-sm" onSubmit={handleFetchOrder} noValidate>
        <div className="min-w-56 flex-1 space-y-2">
          <label className="block text-sm font-bold" htmlFor="return-order-id">شمارهٔ سفارش</label>
          <Input
            id="return-order-id"
            inputMode="numeric"
            dir="ltr"
            value={orderInput}
            onChange={(event) => setOrderInput(event.target.value)}
            aria-invalid={Boolean(orderInputError)}
          />
        </div>
        <Button type="submit" variant="secondary" loading={order.isFetching}>
          دریافت سفارش
        </Button>
        {orderInputError ? <p className="basis-full text-sm text-[var(--danger)]">{orderInputError}</p> : null}
      </form>

      {!orderId ? null : order.isFetching ? (
        <p className="py-8 text-center text-sm text-[var(--muted)]">در حال دریافت سفارش…</p>
      ) : order.error ? (
        <Alert>{presentApiError(order.error)}</Alert>
      ) : order.data ? (
        <FetchedOrder key={order.data.orderId} order={order.data} />
      ) : null}
    </section>
  );
}

function FetchedOrder({ order }: { order: OrderPreview }) {
  const targetEntityId = String(order.orderId);
  const pending = usePendingApprovalForTarget(returnTargetEntityType, targetEntityId);
  const hasPendingRequest = pending.data?.items.some((request) => request.requestType === returnRequestType) ?? false;
  const blockedReason = order.ineligibilityReason
    ?? (hasPendingRequest ? "یک درخواست مرجوعی برای این سفارش در انتظار تأیید است." : undefined);

  return (
    <div className="space-y-5">
      <div className="space-y-4 rounded-xl border border-[var(--border)] bg-white p-4 shadow-sm">
        <p className="text-sm font-bold text-emerald-700">سفارش دریافت شد</p>
        <OrderSummary order={order} />
      </div>

      {blockedReason ? <Alert tone="warning">{blockedReason}</Alert> : null}

      <ReturnDetailsForm order={order} disabled={Boolean(blockedReason) || pending.isPending} />
    </div>
  );
}

function ReturnDetailsForm({ order, disabled }: { order: OrderPreview; disabled: boolean }) {
  const [reason, setReason] = useState("");
  const [returnDate, setReturnDate] = useState("");
  const [errors, setErrors] = useState<Partial<Record<"reason" | "returnDate", string>>>({});
  const [submitError, setSubmitError] = useState<string>();
  const [submitted, setSubmitted] = useState(false);
  const mutation = useCreateApprovalRequest();

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitError(undefined);

    const parsed = returnRequestSchema({ orderCreatedAtUtc: order.createdAtUtc }).safeParse({
      orderId: String(order.orderId),
      reason,
      returnDate,
    });
    if (!parsed.success) {
      const fieldErrors: Partial<Record<"reason" | "returnDate", string>> = {};
      for (const issue of parsed.error.issues) {
        const field = issue.path[0];
        if ((field === "reason" || field === "returnDate") && !fieldErrors[field]) fieldErrors[field] = issue.message;
      }
      setErrors(fieldErrors);
      return;
    }

    setErrors({});
    try {
      await mutation.mutateAsync({
        requestType: returnRequestType,
        targetEntityType: returnTargetEntityType,
        targetEntityId: String(order.orderId),
        reason: parsed.data.reason,
        payload: { orderId: order.orderId, reason: parsed.data.reason, returnDate: parsed.data.returnDate },
      });
      setSubmitted(true);
    } catch (requestError) {
      setSubmitError(presentApiError(requestError));
    }
  }

  if (submitted) {
    return (
      <div className="space-y-2 rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-sm">
        <p className="font-bold text-emerald-800">درخواست ثبت شد و منتظر تأیید سوپرادمین است.</p>
        <Link className="font-bold text-[var(--primary)] hover:underline" href="/returns">مشاهدهٔ فهرست مرجوعی‌ها</Link>
      </div>
    );
  }

  return (
    <form className="space-y-4 rounded-xl border border-[var(--border)] bg-white p-4 shadow-sm" onSubmit={handleSubmit} noValidate>
      {submitError ? <Alert>{submitError}</Alert> : null}
      <fieldset className="space-y-4" disabled={disabled}>
        <div className="space-y-2">
          <label className="block text-sm font-bold" htmlFor="return-reason">علت مرجوعی</label>
          <textarea
            id="return-reason"
            className="min-h-28 w-full rounded-lg border border-[var(--border)] bg-white p-3 text-sm shadow-sm disabled:bg-neutral-50"
            maxLength={returnReasonMaxLength}
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            aria-invalid={Boolean(errors.reason)}
            required
          />
          {errors.reason ? <p className="text-sm text-[var(--danger)]">{errors.reason}</p> : null}
        </div>
        <div className="max-w-xs space-y-2">
          <label className="block text-sm font-bold" htmlFor="return-date">تاریخ عودت (شمسی)</label>
          <Input
            id="return-date"
            dir="ltr"
            placeholder="۱۴۰۵/۰۷/۰۲"
            value={returnDate}
            onChange={(event) => setReturnDate(event.target.value)}
            aria-invalid={Boolean(errors.returnDate)}
            required
          />
          {errors.returnDate ? <p className="text-sm text-[var(--danger)]">{errors.returnDate}</p> : null}
        </div>
        <Button type="submit" loading={mutation.isPending}>
          {mutation.isPending ? "در حال ثبت…" : "ثبت برای تأیید"}
        </Button>
      </fieldset>
    </form>
  );
}
