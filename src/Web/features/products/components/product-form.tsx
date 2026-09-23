"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";

import type { ProductDetail } from "@/features/products/api/get-product";
import type { CreateProductRequest, UpdateProductRequest } from "@/features/products/api/product-editor";
import { productQueryKeys } from "@/features/products/api/list-products";
import { RichTextEditor } from "@/features/products/components/rich-text-editor";
import { TaxonomySelectors } from "@/features/products/components/taxonomy-selectors";
import { useCreateProduct, useProductTaxonomy, useUpdateProduct } from "@/features/products/hooks/use-product-editor";
import { createRichTextState, type RichTextState } from "@/features/products/lib/rich-text-state";
import { createIdempotencyKey } from "@/features/products/lib/idempotency-key";
import { createVariantSchema, productEditorSchema, selectedFilterIds, validateProductKind } from "@/features/products/schemas/product-editor-schema";
import { ApiError, presentApiError } from "@/shared/lib/api-client";
import { formatPersianDateTime, formatPrice, formatPersianNumber } from "@/shared/lib/formatters";
import { Alert } from "@/shared/ui/alert";
import { Badge } from "@/shared/ui/badge";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { LoadingOverlay } from "@/shared/ui/loading-overlay";
import { SaveBar } from "@/shared/ui/save-bar";

type NamedValue = { name: string; value: string };
type ContentValue = NamedValue & { editor: RichTextState };
type VariantValue = { title: string; price: string; stock: string; sku: string };

export function ProductForm({ mode, product }: { mode: "create" | "edit"; product?: ProductDetail }) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const taxonomy = useProductTaxonomy();
  const createMutation = useCreateProduct();
  const updateMutation = useUpdateProduct(product?.id ?? "");
  const mutationPending = createMutation.isPending || updateMutation.isPending;
  const [idempotencyKey] = useState(createIdempotencyKey);
  const [error, setError] = useState<string>();
  const [kind, setKind] = useState<"simple" | "variable">(
    product && product.attributes.length > 0 ? "variable" : "simple",
  );
  const [description, setDescription] = useState(() => createRichTextState(product?.description));
  const [contents, setContents] = useState<ContentValue[]>(() =>
    (product?.contents.length ? product.contents : [{ name: "معرفی", value: "" }]).map((item) => ({
      name: item.name,
      value: item.value ?? "",
      editor: createRichTextState(item.value),
    })),
  );
  const [fields, setFields] = useState<NamedValue[]>(() =>
    product?.fields.map((item) => ({ name: item.name, value: item.value ?? "" })) ?? [],
  );
  const [attributes, setAttributes] = useState<NamedValue[]>(() =>
    product?.attributes.map((item) => ({ name: item.name, value: item.value.join("، ") })) ?? [],
  );
  const [variants, setVariants] = useState<VariantValue[]>(() =>
    mode === "create" ? [{ title: "primary", price: "", stock: "", sku: "" }] : [],
  );
  const [categoryIds, setCategoryIds] = useState(() => new Set(product?.categories.map((item) => item.id) ?? []));
  const [filterIds, setFilterIds] = useState(() => new Set(product?.filters.map((item) => item.id) ?? []));

  const unavailable = Boolean(
    product?.isStale || taxonomy.isStale || taxonomy.categories.isError || taxonomy.filters.isError,
  );

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(undefined);
    const data = new FormData(event.currentTarget);
    const common = productEditorSchema.safeParse({
      title: data.get("title"),
      caption: data.get("caption"),
      slug: data.get("slug"),
      metaTitle: data.get("metaTitle"),
      metaDescription: data.get("metaDescription"),
      metaKeywords: data.get("metaKeywords"),
      metaRobots: data.get("metaRobots"),
      redirect: data.get("redirect"),
    });
    if (!common.success) {
      setError(common.error.issues[0]?.message ?? "اطلاعات محصول را بررسی کنید.");
      return;
    }

    try {
      if (mode === "create") {
        const kindError = validateProductKind(kind, variants.map((variant) => variant.title.trim()));
        if (kindError) throw new Error(kindError);
        const parsedVariants = variants.map((variant) => createVariantSchema.safeParse(variant));
        const invalid = parsedVariants.find((result) => !result.success);
        if (invalid && !invalid.success) throw new Error(invalid.error.issues[0]?.message ?? "واریانت نامعتبر است.");

        const request: CreateProductRequest = {
          ...nullableCommon(common.data),
          description: description.value || null,
          contents: contents.map((item) => ({ name: item.name, value: item.editor.value || null })),
          commentingEnabled: data.get("commentingEnabled") === "on",
          fields: fields.map(toApiNameValue),
          categoryIds: [...categoryIds].map(Number),
          filterIds: selectedFilterIds(filterIds),
          attributes: kind === "variable" ? attributes.map((item) => ({
            name: item.name,
            value: item.value.split(/[،,]/).map((value) => value.trim()).filter(Boolean),
          })) : [],
          variants: parsedVariants.map((result) => {
            if (!result.success) throw new Error("واریانت نامعتبر است.");
            return {
              title: result.data.title,
              price: result.data.price,
              comparePrice: null,
              tax: null,
              shipping: null,
              weight: null,
              length: null,
              width: null,
              height: null,
              stock: result.data.stock,
              minimum: null,
              maximum: null,
              sku: result.data.sku || null,
            };
          }),
        };
        const result = await createMutation.mutateAsync({ request, idempotencyKey });
        router.push(`/products/${encodeURIComponent(result.id)}`);
        return;
      }

      if (!product?.version) throw new Error("نسخهٔ محصول در پاسخ پرتال موجود نیست.");
      const request: UpdateProductRequest = {
        ...nullableCommon(common.data),
        lastKnownVersion: product.version,
        description: description.value || null,
        descriptionDirty: description.dirty,
        contents: contents.map((item) => ({ name: item.name, value: item.editor.value || null, isDirty: item.editor.dirty })),
        commentingEnabled: data.get("commentingEnabled") === "on",
        fields: fields.map(toApiNameValue),
        categoryIds: [...categoryIds].map(Number),
        filterIds: selectedFilterIds(filterIds),
      };
      await updateMutation.mutateAsync(request);
      router.push(`/products/${encodeURIComponent(product.id)}`);
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.status === 409 && product) {
        await queryClient.invalidateQueries({ queryKey: productQueryKeys.detail(product.id) });
        setError(`${presentApiError(requestError)} دادهٔ تازه دریافت شد؛ صفحه را دوباره بارگذاری کنید.`);
      } else {
        setError(requestError instanceof Error && !(requestError instanceof ApiError)
          ? requestError.message
          : presentApiError(requestError));
      }
    }
  }

  return (
    <form className="relative space-y-6 pb-24" onSubmit={handleSubmit} noValidate aria-busy={mutationPending}>
      {mutationPending ? <LoadingOverlay label="در حال ذخیرهٔ محصول…" /> : null}

      <header className="space-y-1">
        <h1 className="text-2xl font-black">{mode === "create" ? "ایجاد محصول" : "ویرایش محصول"}</h1>
        <p className="text-sm text-[var(--muted)]">قیمت و موجودی محصول موجود فقط از بخش واریانت‌ها تغییر می‌کند.</p>
      </header>

      {error ? <Alert>{error}</Alert> : null}
      {unavailable ? <Alert tone="warning">دادهٔ پرتال در دسترس نیست یا کهنه است؛ ذخیره تا برقراری ارتباط غیرفعال است.</Alert> : null}

      <section className="grid gap-4 rounded-xl border border-[var(--border)] bg-white p-5 md:grid-cols-2">
        <Field label="عنوان" name="title" defaultValue={product?.title ?? ""} required />
        <Field label="زیرعنوان" name="caption" defaultValue={product?.caption ?? ""} />
        <Field label="نامک" name="slug" defaultValue={product?.slug ?? ""} />
        <label className="flex items-center gap-2 self-end pb-3 text-sm font-bold">
          <input type="checkbox" name="commentingEnabled" defaultChecked={product?.commentingEnabled ?? false} />
          دیدگاه‌ها فعال باشند
        </label>
      </section>

      <section className="space-y-4 rounded-xl border border-[var(--border)] bg-white p-5">
        <RichTextEditor id="description" label="توضیحات" state={description} onChange={setDescription} />
        <h2 className="font-black">بخش‌های محتوا</h2>
        {contents.map((item, index) => (
          <div key={`${index}-${item.name}`} className="space-y-3 border-t border-[var(--border)] pt-4">
            <Input value={item.name} aria-label="نام بخش محتوا" onChange={(event) => setContents(replaceAt(contents, index, { ...item, name: event.target.value }))} />
            <RichTextEditor
              id={`content-${index}`}
              label={`محتوای ${item.name || index + 1}`}
              state={item.editor}
              onChange={(editor) => setContents(replaceAt(contents, index, { ...item, editor }))}
            />
            <Button type="button" variant="ghost" onClick={() => setContents(removeAt(contents, index))}>حذف بخش</Button>
          </div>
        ))}
        <Button type="button" variant="secondary" onClick={() => setContents([...contents, { name: "", value: "", editor: createRichTextState("") }])}>افزودن بخش</Button>
      </section>

      <NamedValuesEditor title="فیلدهای محصول" values={fields} onChange={setFields} />

      <section className="grid gap-4 rounded-xl border border-[var(--border)] bg-white p-5 md:grid-cols-2">
        <Field label="عنوان متا" name="metaTitle" defaultValue={product?.metaTitle ?? ""} />
        <Field label="کلیدواژه‌های متا" name="metaKeywords" defaultValue={product?.metaKeywords ?? ""} />
        <Field label="robots" name="metaRobots" defaultValue={product?.metaRobots ?? ""} />
        <Field label="Redirect" name="redirect" defaultValue={product?.redirect ?? ""} />
        <div className="md:col-span-2"><Field label="توضیح متا" name="metaDescription" defaultValue={product?.metaDescription ?? ""} /></div>
      </section>

      <TaxonomySelectors
        categories={taxonomy.categories.data?.items ?? []}
        filters={taxonomy.filters.data?.items ?? []}
        categoryIds={categoryIds}
        filterIds={filterIds}
        onCategoryIds={setCategoryIds}
        onFilterIds={setFilterIds}
      />

      {mode === "create" ? (
        <CreateVariants kind={kind} onKindChange={(value) => {
          setKind(value);
          if (value === "simple") setVariants([{ title: "primary", price: "", stock: "", sku: "" }]);
        }} attributes={attributes} onAttributes={setAttributes} variants={variants} onVariants={setVariants} />
      ) : product ? <ReadOnlyProductData product={product} /> : null}

      <SaveBar
        message={error}
        loading={mutationPending}
        disabled={unavailable || taxonomy.categories.isPending || taxonomy.filters.isPending}
      />
    </form>
  );
}

function nullableCommon(value: { title: string; caption: string; slug: string; metaTitle: string; metaDescription: string; metaKeywords: string; metaRobots: string; redirect: string }) {
  return {
    title: value.title,
    caption: value.caption || null,
    slug: value.slug || null,
    metaTitle: value.metaTitle || null,
    metaDescription: value.metaDescription || null,
    metaKeywords: value.metaKeywords || null,
    metaRobots: value.metaRobots || null,
    redirect: value.redirect || null,
  };
}

function toApiNameValue(item: NamedValue) { return { name: item.name, value: item.value || null }; }

function replaceAt<T>(values: T[], index: number, value: T) { return values.map((item, current) => current === index ? value : item); }
function removeAt<T>(values: T[], index: number) { return values.filter((_, current) => current !== index); }

function Field({ label, name, defaultValue, required = false }: { label: string; name: string; defaultValue: string; required?: boolean }) {
  return <div className="space-y-2"><label className="text-sm font-bold" htmlFor={name}>{label}</label><Input id={name} name={name} defaultValue={defaultValue} required={required} /></div>;
}

function NamedValuesEditor({ title, values, onChange }: { title: string; values: NamedValue[]; onChange: (value: NamedValue[]) => void }) {
  return (
    <section className="space-y-3 rounded-xl border border-[var(--border)] bg-white p-5">
      <h2 className="font-black">{title}</h2>
      {values.map((item, index) => <div className="grid gap-2 md:grid-cols-[1fr_2fr_auto]" key={index}>
        <Input aria-label="نام" value={item.name} onChange={(event) => onChange(replaceAt(values, index, { ...item, name: event.target.value }))} />
        <Input aria-label="مقدار" value={item.value} onChange={(event) => onChange(replaceAt(values, index, { ...item, value: event.target.value }))} />
        <Button type="button" variant="ghost" onClick={() => onChange(removeAt(values, index))}>حذف</Button>
      </div>)}
      <Button type="button" variant="secondary" onClick={() => onChange([...values, { name: "", value: "" }])}>افزودن</Button>
    </section>
  );
}

function CreateVariants({ kind, onKindChange, attributes, onAttributes, variants, onVariants }: {
  kind: "simple" | "variable"; onKindChange: (value: "simple" | "variable") => void;
  attributes: NamedValue[]; onAttributes: (value: NamedValue[]) => void;
  variants: VariantValue[]; onVariants: (value: VariantValue[]) => void;
}) {
  return <section className="space-y-4 rounded-xl border border-[var(--border)] bg-white p-5">
    <h2 className="font-black">نوع و واریانت‌های اولیه</h2>
    <div className="flex gap-5"><label><input type="radio" checked={kind === "simple"} onChange={() => onKindChange("simple")} /> کالای ساده</label><label><input type="radio" checked={kind === "variable"} onChange={() => onKindChange("variable")} /> کالای چندواریانتی</label></div>
    {kind === "variable" ? <NamedValuesEditor title="ویژگی‌ها؛ مقادیر را با ویرگول جدا کنید" values={attributes} onChange={onAttributes} /> : null}
    {variants.map((variant, index) => <div className="grid gap-2 border-t border-[var(--border)] pt-4 md:grid-cols-4" key={index}>
      <Input aria-label="عنوان واریانت" value={variant.title} disabled={kind === "simple"} onChange={(event) => onVariants(replaceAt(variants, index, { ...variant, title: event.target.value }))} />
      <Input aria-label="قیمت" inputMode="decimal" placeholder="قیمت تومان" value={variant.price} onChange={(event) => onVariants(replaceAt(variants, index, { ...variant, price: event.target.value }))} />
      <Input aria-label="موجودی" inputMode="numeric" placeholder="موجودی" value={variant.stock} onChange={(event) => onVariants(replaceAt(variants, index, { ...variant, stock: event.target.value }))} />
      <Input aria-label="SKU" value={variant.sku} onChange={(event) => onVariants(replaceAt(variants, index, { ...variant, sku: event.target.value }))} />
      {kind === "variable" ? <Button type="button" variant="ghost" onClick={() => onVariants(removeAt(variants, index))}>حذف واریانت</Button> : null}
    </div>)}
    {kind === "variable" ? <Button type="button" variant="secondary" onClick={() => onVariants([...variants, { title: "", price: "", stock: "", sku: "" }])}>افزودن واریانت</Button> : null}
  </section>;
}

function ReadOnlyProductData({ product }: { product: ProductDetail }) {
  return <section className="space-y-4 rounded-xl border border-[var(--border)] bg-white p-5">
    <h2 className="font-black">اطلاعات فقط‌خواندنی</h2>
    <p className="text-sm">نشانی canonical: {product.canonicalUrl ?? "تعریف نشده"}</p>
    <p className="text-sm">تاریخ انتشار: {formatPersianDateTime(product.publishedAtUtc)}</p>
    <div className="flex flex-wrap gap-2">{product.statuses.map((status) => <Badge tone="neutral" key={status}>{statusLabel(status)}</Badge>)}</div>
    <p className="text-sm">تصاویر: {formatPersianNumber(product.images.length)} تصویر؛ در این مرحله قابل تغییر نیستند.</p>
    <div className="space-y-2"><h3 className="font-bold">واریانت‌ها</h3>{product.variants.map((variant) => <div className="rounded-lg bg-neutral-50 p-3 text-sm" key={variant.id ?? variant.title ?? "variant"}>{variant.title ?? "بدون عنوان"} — {formatPrice(variant.price)} — موجودی {variant.stock == null ? "تعریف نشده" : formatPersianNumber(variant.stock)}</div>)}</div>
  </section>;
}

function statusLabel(status: string) {
  return ({ pending: "منتظر بررسی", approved: "منتشرشده", suspended: "منتشرنشود", available: "موجود", unavailable: "ناموجود", featured: "ویژه", most: "پربازدید", sale: "تخفیف" } as Record<string, string>)[status] ?? status;
}
