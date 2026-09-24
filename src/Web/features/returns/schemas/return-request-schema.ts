import { z } from "zod";

import { jalaliToIsoDate, parseJalaliDate, todayJalali } from "../../../shared/lib/jalali-date.ts";
import { normalizeNumericInput } from "../../../shared/lib/normalize-number.ts";

// Mirrors the backend's RegisterReturnPayloadValidator (FluentValidation);
// "not before the order was created" is checked here only when the order is
// known, and always again on the server.

export const returnReasonMaxLength = 2000;

export const orderIdSchema = z.preprocess(
  normalizeNumericInput,
  z.string().regex(/^[1-9]\d*$/, "شمارهٔ سفارش باید یک عدد صحیح مثبت باشد."),
);

export function returnRequestSchema({ now = new Date(), orderCreatedAtUtc }: { now?: Date; orderCreatedAtUtc?: string | null } = {}) {
  const today = jalaliToIsoDate(todayJalali(now))!;
  const orderDay = orderCreatedAtUtc ? jalaliToIsoDate(todayJalali(new Date(orderCreatedAtUtc))) : undefined;

  return z.object({
    orderId: orderIdSchema,
    reason: z
      .string()
      .trim()
      .min(1, "علت مرجوعی الزامی است.")
      .max(returnReasonMaxLength, "علت مرجوعی حداکثر ۲۰۰۰ کاراکتر است."),
    returnDate: z
      .string()
      .trim()
      .min(1, "تاریخ عودت الزامی است.")
      .refine((value) => parseJalaliDate(value) !== null, "تاریخ شمسی معتبر نیست؛ مثلاً ۱۴۰۵/۰۷/۰۲.")
      .transform((value) => jalaliToIsoDate(value)!)
      .refine((isoDate) => isoDate <= today, "تاریخ عودت نمی‌تواند در آینده باشد.")
      .refine((isoDate) => !orderDay || isoDate >= orderDay, "تاریخ عودت نمی‌تواند پیش از تاریخ ثبت سفارش باشد."),
  });
}

export type ReturnRequestInput = z.input<ReturnType<typeof returnRequestSchema>>;

/** The approval payload of ADR-054 — only these three fields; the order's contents are read by the server. */
export type ReturnRequestPayload = z.output<ReturnType<typeof returnRequestSchema>>;
