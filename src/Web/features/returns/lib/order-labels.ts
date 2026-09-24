// The portal's English order statuses shown with Persian labels; an unknown
// status is shown by its raw name rather than hidden or guessed.
const orderStatusLabels: Record<string, string> = {
  paid: "پرداخت‌شده",
  fulfilled: "تکمیل‌شده",
  canceled: "لغوشده",
  shipping_required: "نیازمند ارسال",
};

export function orderStatusLabel(status: string) {
  return orderStatusLabels[status] ?? status;
}

/** Unit price × quantity for one order line; the amounts are the portal's raw numbers (rule 13). */
export function lineTotal(price: number | string | null | undefined, quantity: number | string) {
  return price == null ? null : Number(price) * Number(quantity);
}
