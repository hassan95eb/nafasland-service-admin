"use client";

import { useRouter } from "next/navigation";
import Link from "next/link";
import { useEffect, useMemo, useState } from "react";

import { ProductsTable } from "@/features/products/components/products-table";
import { useDebouncedValue } from "@/features/products/hooks/use-debounced-value";
import { useProducts } from "@/features/products/hooks/use-products";
import { filterProductsWithoutPrice } from "@/features/products/schemas/product-filters";
import { ApiError, presentApiError } from "@/shared/lib/api-client";
import { formatPersianDateTime, formatPersianNumber } from "@/shared/lib/formatters";
import { Pagination } from "@/shared/data-table/pagination";
import { Alert } from "@/shared/ui/alert";
import { Input } from "@/shared/ui/input";
import { Can } from "@/shared/permissions/permission-context";

const pageSize = 25;

export function ProductList() {
  const router = useRouter();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [onlyWithoutPrice, setOnlyWithoutPrice] = useState(false);
  const keywords = useDebouncedValue(search.trim(), 300);
  const query = useProducts({ page, pageSize, keywords });

  useEffect(() => {
    if (query.error instanceof ApiError && query.error.status === 401) {
      router.replace("/login");
      router.refresh();
    }
  }, [query.error, router]);

  const displayedProducts = useMemo(
    () => filterProductsWithoutPrice(query.data?.items ?? [], onlyWithoutPrice),
    [onlyWithoutPrice, query.data?.items],
  );
  const pageCount = Math.ceil(Number(query.data?.total ?? 0) / pageSize);

  return (
    <section className="space-y-5">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1">
          <h1 className="text-2xl font-black">محصولات</h1>
          <p className="text-sm text-[var(--muted)]">فهرست زندهٔ نفس‌لند؛ {formatPersianNumber(query.data?.total ?? 0)} محصول</p>
        </div>
        <Can permission="catalog.products.write">
          <Link className="rounded-lg bg-[var(--primary)] px-4 py-2 text-sm font-bold text-white" href="/products/new">ایجاد محصول</Link>
        </Can>
      </header>

      <div className="grid gap-3 rounded-xl border border-[var(--border)] bg-white p-4 md:grid-cols-[minmax(0,1fr)_auto] md:items-center">
        <div>
          <label className="sr-only" htmlFor="product-search">
            جست‌وجوی محصول
          </label>
          <Input
            id="product-search"
            type="search"
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              setPage(1);
            }}
            placeholder="جست‌وجو در عنوان یا شناسهٔ محصول…"
          />
        </div>
        <label className="flex cursor-pointer items-center gap-2 text-sm font-bold">
          <input
            className="size-4 accent-[var(--primary)]"
            type="checkbox"
            checked={onlyWithoutPrice}
            onChange={(event) => setOnlyWithoutPrice(event.target.checked)}
          />
          فقط محصولات بدون قیمت
        </label>
        {onlyWithoutPrice ? (
          <p className="text-xs leading-6 text-[var(--muted)] md:col-span-2">
            این فیلتر فقط محصولات صفحهٔ فعلی را بررسی می‌کند.
          </p>
        ) : null}
      </div>

      {query.error && !(query.error instanceof ApiError && query.error.status === 401) ? (
        <Alert>{presentApiError(query.error)}</Alert>
      ) : null}

      {query.data?.isStale ? (
        <Alert tone="warning">
          ارتباط با فروشگاه برقرار نیست؛ داده‌های نمایش‌داده‌شده مربوط به {formatPersianDateTime(query.data.asOfUtc)} است.
        </Alert>
      ) : null}

      <div className="overflow-hidden rounded-xl border border-[var(--border)] bg-white shadow-sm" aria-busy={query.isFetching}>
        {query.isPending ? (
          <p className="px-4 py-12 text-center text-sm text-[var(--muted)]">در حال دریافت محصولات…</p>
        ) : (
          <ProductsTable products={displayedProducts} />
        )}
        <Pagination
          page={page}
          pageCount={pageCount}
          disabled={query.isFetching}
          onPageChange={setPage}
        />
      </div>
    </section>
  );
}
