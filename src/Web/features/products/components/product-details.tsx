"use client";

import { useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useState, type FormEvent } from "react";

import type { ProductVariant } from "@/features/products/api/get-product";
import { productQueryKeys } from "@/features/products/api/list-products";
import { createIdempotencyKey } from "@/features/products/lib/idempotency-key";
import { useProduct, useUpdateVariant } from "@/features/products/hooks/use-product";
import { updateVariantSchema } from "@/features/products/schemas/update-variant-schema";
import { SafeProductHtml } from "@/features/products/components/safe-product-html";
import { ApiError, presentApiError } from "@/shared/lib/api-client";
import { formatPersianDateTime, formatPrice, formatPersianNumber } from "@/shared/lib/formatters";
import { Can } from "@/shared/permissions/permission-context";
import { Alert } from "@/shared/ui/alert";
import { Badge } from "@/shared/ui/badge";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";

export function ProductDetails({ id }: { id: string }) {
  const query = useProduct(id);

  if (query.isPending) {
    return <p className="py-12 text-center text-sm text-[var(--muted)]">در حال دریافت جزئیات محصول…</p>;
  }
  if (query.error || !query.data) {
    return <Alert>{presentApiError(query.error)}</Alert>;
  }

  const product = query.data;
  return (
    <section className="space-y-6">
      <Link className="text-sm font-bold text-[var(--primary)]" href="/products">بازگشت به فهرست محصولات</Link>

      {product.isStale ? (
        <Alert tone="warning">
          ارتباط با فروشگاه برقرار نیست؛ داده‌های نمایش‌داده‌شده مربوط به {formatPersianDateTime(product.asOfUtc)} است.
          ویرایش تا برقراری دوبارهٔ ارتباط غیرفعال است.
        </Alert>
      ) : null}

      <header className="rounded-xl border border-[var(--border)] bg-white p-5 shadow-sm">
        <h1 className="text-2xl font-black">{product.title || "محصول بدون عنوان"}</h1>
        <p className="mt-2 text-sm text-[var(--muted)]">شناسهٔ محصول: {product.id}</p>
        <Can permission="catalog.products.write">
          <Link className="mt-4 inline-flex rounded-lg bg-[var(--primary)] px-4 py-2 text-sm font-bold text-white" href={`/products/${encodeURIComponent(product.id)}/edit`}>
            ویرایش محصول
          </Link>
        </Can>
      </header>

      {product.description ? <section className="rounded-xl border border-[var(--border)] bg-white p-5"><SafeProductHtml html={product.description} /></section> : null}

      <div className="space-y-4">
        <h2 className="text-lg font-black">قیمت و موجودی واریانت‌ها</h2>
        {product.variants.length === 0 ? (
          <p className="rounded-xl border border-[var(--border)] bg-white p-5 text-sm text-[var(--muted)]">این محصول واریانتی ندارد.</p>
        ) : product.variants.map((variant, index) => (
          <VariantCard key={variant.id ?? `${variant.title}-${index}`} productId={product.id} variant={variant} disabled={product.isStale || !variant.id} />
        ))}
      </div>
    </section>
  );
}

function VariantCard({
  productId,
  variant,
  disabled,
}: {
  productId: string;
  variant: ProductVariant;
  disabled: boolean;
}) {
  const [editing, setEditing] = useState(false);

  return (
    <article className="space-y-4 rounded-xl border border-[var(--border)] bg-white p-5 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-2">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="font-black">{variant.sku || `واریانت ${variant.id}`}</h3>
            <Badge tone={Number(variant.stock ?? 0) > 0 ? "success" : "neutral"}>
              {Number(variant.stock ?? 0) > 0 ? "موجود" : "ناموجود"}
            </Badge>
          </div>
          <p className="text-sm">قیمت: {formatPrice(variant.price)}</p>
          <p className="text-sm">موجودی: {variant.stock == null ? "تعریف نشده" : formatPersianNumber(variant.stock)}</p>
          <p className="text-xs text-[var(--muted)]">شناسهٔ واریانت: {variant.id}</p>
        </div>
        <Can permission="catalog.products.write">
          <Button type="button" variant="secondary" disabled={disabled} onClick={() => setEditing((value) => !value)}>
            {editing ? "بستن فرم" : "ویرایش قیمت و موجودی"}
          </Button>
        </Can>
      </div>

      {editing ? (
        <Can permission="catalog.products.write">
          <VariantEditor productId={productId} variant={variant} disabled={disabled} onSaved={() => setEditing(false)} />
        </Can>
      ) : null}
    </article>
  );
}

function VariantEditor({
  productId,
  variant,
  disabled,
  onSaved,
}: {
  productId: string;
  variant: ProductVariant;
  disabled: boolean;
  onSaved: () => void;
}) {
  const mutation = useUpdateVariant(productId);
  const queryClient = useQueryClient();
  const [idempotencyKey] = useState(createIdempotencyKey);
  const [error, setError] = useState<string>();

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(undefined);
    if (!variant.id) {
      setError("شناسهٔ واریانت در پاسخ پرتال موجود نیست؛ ذخیره انجام نشد.");
      return;
    }
    const data = new FormData(event.currentTarget);
    const parsed = updateVariantSchema.safeParse({
      newPrice: data.get("newPrice"),
      newStock: data.get("newStock"),
    });
    if (!parsed.success) {
      setError(parsed.error.issues[0]?.message ?? "مقادیر قیمت و موجودی را بررسی کنید.");
      return;
    }

    try {
      await mutation.mutateAsync({
        variantId: variant.id,
        idempotencyKey,
        request: {
          ...parsed.data,
          lastKnownPrice: variant.price,
          lastKnownStock: variant.stock,
        },
      });
      onSaved();
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.status === 409) {
        void queryClient.invalidateQueries({ queryKey: productQueryKeys.detail(productId) });
        setError(`${requestError.message} داده‌های صفحه دوباره دریافت می‌شوند؛ پیش از تلاش بعدی فرم را دوباره باز کنید.`);
      } else {
        setError(presentApiError(requestError));
      }
    }
  }

  return (
    <form className="grid gap-4 border-t border-[var(--border)] pt-4 md:grid-cols-2" onSubmit={handleSubmit} noValidate>
      {error ? <div className="md:col-span-2"><Alert>{error}</Alert></div> : null}
      <div className="space-y-2">
        <label className="block text-sm font-bold" htmlFor={`price-${variant.id}`}>قیمت (تومان)</label>
        <Input id={`price-${variant.id}`} name="newPrice" inputMode="decimal" defaultValue={variant.price ?? ""} disabled={disabled} />
      </div>
      <div className="space-y-2">
        <label className="block text-sm font-bold" htmlFor={`stock-${variant.id}`}>موجودی</label>
        <Input id={`stock-${variant.id}`} name="newStock" inputMode="numeric" defaultValue={variant.stock ?? ""} disabled={disabled} />
      </div>
      <div className="md:col-span-2">
        <Button type="submit" disabled={disabled || mutation.isPending}>
          {mutation.isPending ? "در حال ذخیره…" : "ذخیرهٔ قیمت و موجودی"}
        </Button>
      </div>
    </form>
  );
}
