import type { OrderItem } from "@/features/returns/api/returns-api";
import { lineTotal, orderStatusLabel } from "@/features/returns/lib/order-labels";
import { formatPersianDate, formatPersianNumber, formatPrice } from "@/shared/lib/formatters";
import { Badge } from "@/shared/ui/badge";

export interface OrderSummaryData {
  customerName?: string | null;
  statuses: string[];
  createdAtUtc?: string | null;
  items: OrderItem[];
  subtotal?: number | string | null;
  shipping?: number | string | null;
  discount?: number | string | null;
  tax?: number | string | null;
  total?: number | string | null;
}

/** The order as read from the portal (or as snapshotted at approval): lines, amounts and the only customer datum kept, the name. */
export function OrderSummary({ order }: { order: OrderSummaryData }) {
  return (
    <div className="space-y-4">
      <dl className="grid gap-3 text-sm sm:grid-cols-3">
        <div>
          <dt className="text-xs text-[var(--muted)]">نام مشتری</dt>
          <dd className="font-bold">{order.customerName ?? "نامشخص"}</dd>
        </div>
        <div>
          <dt className="text-xs text-[var(--muted)]">تاریخ سفارش</dt>
          <dd className="font-bold">{formatPersianDate(order.createdAtUtc)}</dd>
        </div>
        <div>
          <dt className="text-xs text-[var(--muted)]">وضعیت سفارش</dt>
          <dd className="flex flex-wrap gap-1">
            {order.statuses.length === 0 ? "—" : order.statuses.map((status) => (
              <Badge key={status} tone={status === "canceled" ? "danger" : "neutral"}>{orderStatusLabel(status)}</Badge>
            ))}
          </dd>
        </div>
      </dl>

      <div className="overflow-x-auto rounded-xl border border-[var(--border)]">
        <table className="w-full min-w-[560px] border-collapse text-right text-sm">
          <thead className="bg-neutral-50 text-xs text-[var(--muted)]">
            <tr>
              <th className="px-4 py-3 font-bold">عنوان محصول</th>
              <th className="px-4 py-3 font-bold">تعداد</th>
              <th className="px-4 py-3 font-bold">قیمت واحد</th>
              <th className="px-4 py-3 font-bold">جمع ردیف</th>
            </tr>
          </thead>
          <tbody>
            {order.items.length === 0 ? (
              <tr>
                <td colSpan={4} className="px-4 py-6 text-center text-[var(--muted)]">این سفارش قلمی ندارد.</td>
              </tr>
            ) : (
              order.items.map((item, index) => (
                <tr key={`${item.variantId ?? "item"}-${index}`} className="border-t border-[var(--border)]">
                  <td className="px-4 py-3">{item.title ?? "بدون عنوان"}</td>
                  <td className="px-4 py-3">{formatPersianNumber(item.quantity)}</td>
                  <td className="whitespace-nowrap px-4 py-3">{formatPrice(item.price)}</td>
                  <td className="whitespace-nowrap px-4 py-3">{formatPrice(lineTotal(item.price, item.quantity))}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <dl className="mr-auto grid max-w-sm grid-cols-2 gap-x-6 gap-y-2 text-sm">
        <dt className="text-[var(--muted)]">جمع اقلام</dt>
        <dd>{formatPrice(order.subtotal)}</dd>
        <dt className="text-[var(--muted)]">هزینهٔ ارسال</dt>
        <dd>{formatPrice(order.shipping)}</dd>
        <dt className="text-[var(--muted)]">تخفیف</dt>
        <dd>{formatPrice(order.discount)}</dd>
        {order.tax && Number(order.tax) !== 0 ? (
          <>
            <dt className="text-[var(--muted)]">مالیات</dt>
            <dd>{formatPrice(order.tax)}</dd>
          </>
        ) : null}
        <dt className="font-bold">مبلغ کل</dt>
        <dd className="font-bold">{formatPrice(order.total)}</dd>
      </dl>
    </div>
  );
}
