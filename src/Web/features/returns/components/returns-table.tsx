import type { ReturnRecordSummary } from "@/features/returns/api/returns-api";
import { formatPersianDate, formatPersianDateTime, formatPrice } from "@/shared/lib/formatters";

export function userLabel(username: string | null | undefined) {
  return username ?? "کاربر ناشناس";
}

export function ReturnsTable({
  items,
  onSelect,
  selectedId,
}: {
  items: ReturnRecordSummary[];
  onSelect: (id: string) => void;
  selectedId: string | null;
}) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[860px] border-collapse text-right text-sm">
        <thead className="bg-neutral-50 text-xs text-[var(--muted)]">
          <tr>
            <th className="px-4 py-3 font-bold">شمارهٔ سفارش</th>
            <th className="px-4 py-3 font-bold">نام مشتری</th>
            <th className="px-4 py-3 font-bold">مبلغ کل</th>
            <th className="px-4 py-3 font-bold">تاریخ عودت</th>
            <th className="px-4 py-3 font-bold">ثبت‌کننده</th>
            <th className="px-4 py-3 font-bold">تأییدکننده</th>
            <th className="px-4 py-3 font-bold">تاریخ تأیید</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr
              key={item.id}
              className={`cursor-pointer border-t border-[var(--border)] hover:bg-emerald-50/60 ${selectedId === item.id ? "bg-emerald-50" : ""}`}
              onClick={() => onSelect(item.id)}
            >
              <td className="px-4 py-3">
                <button
                  type="button"
                  className="font-bold hover:underline"
                  dir="ltr"
                  onClick={(event) => {
                    event.stopPropagation();
                    onSelect(item.id);
                  }}
                >
                  {item.orderId}
                </button>
              </td>
              <td className="px-4 py-3">{item.customerName ?? "نامشخص"}</td>
              <td className="whitespace-nowrap px-4 py-3">{formatPrice(item.total)}</td>
              <td className="whitespace-nowrap px-4 py-3">{formatPersianDate(item.returnDate)}</td>
              <td className="px-4 py-3">{userLabel(item.registeredByUsername)}</td>
              <td className="px-4 py-3">{userLabel(item.approvedByUsername)}</td>
              <td className="whitespace-nowrap px-4 py-3">{formatPersianDateTime(item.approvedAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
