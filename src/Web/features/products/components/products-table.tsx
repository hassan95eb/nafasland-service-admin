import type { ProductSummary } from "@/features/products/api/list-products";
import { formatPersianDate, formatPrice } from "@/shared/lib/formatters";
import { Badge } from "@/shared/ui/badge";

export function ProductsTable({ products }: { products: ProductSummary[] }) {
  if (products.length === 0) {
    return <p className="px-4 py-12 text-center text-sm text-[var(--muted)]">محصولی در این صفحه پیدا نشد.</p>;
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[760px] border-collapse text-right text-sm">
        <thead className="bg-neutral-50 text-xs text-[var(--muted)]">
          <tr>
            <th className="px-4 py-3 font-bold">محصول</th>
            <th className="px-4 py-3 font-bold">قیمت</th>
            <th className="px-4 py-3 font-bold">وضعیت محتوا</th>
            <th className="px-4 py-3 font-bold">دسترسی</th>
            <th className="px-4 py-3 font-bold">تاریخ ایجاد</th>
          </tr>
        </thead>
        <tbody>
          {products.map((product) => (
            <tr className="border-t border-[var(--border)]" key={product.id}>
              <td className="px-4 py-4">
                <Link className="max-w-sm font-bold text-[var(--primary)] hover:underline" href={`/products/${encodeURIComponent(product.id)}`}>
                  {product.title || "بدون عنوان"}
                </Link>
                <p className="mt-1 text-xs text-[var(--muted)]">شناسه: {product.id}</p>
              </td>
              <td className="whitespace-nowrap px-4 py-4">{formatPrice(product.price)}</td>
              <td className="px-4 py-4">
                <Badge tone={product.isPending ? "warning" : "success"}>
                  {product.isPending ? "در انتظار تأیید" : "تأییدشده"}
                </Badge>
              </td>
              <td className="px-4 py-4">
                <Badge tone={product.isAvailable ? "success" : "neutral"}>
                  {product.isAvailable ? "موجود" : "ناموجود"}
                </Badge>
              </td>
              <td className="whitespace-nowrap px-4 py-4">{formatPersianDate(product.createdAtUtc)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
import Link from "next/link";
