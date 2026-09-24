"use client";

import { useEffect, useRef, useState, type FormEvent } from "react";
import { createPortal } from "react-dom";

import { buildVariantTitles, completeAttributes } from "@/features/products/schemas/product-editor-schema";
import { formatPersianNumber } from "@/shared/lib/formatters";
import { normalizeNumericInput } from "@/shared/lib/normalize-number";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";

export type AttributeRow = { name: string; value: string };
export type VariantValue = {
  title: string;
  price: string;
  comparePrice: string;
  stock: string;
  minimum: string;
  maximum: string;
  weight: string;
  length: string;
  width: string;
  height: string;
  sku: string;
};
type VariantField = Exclude<keyof VariantValue, "title">;

export const PRIMARY_TITLE = "primary";

export function emptyVariant(title: string, price = "", comparePrice = ""): VariantValue {
  return { title, price, comparePrice, stock: "", minimum: "", maximum: "", weight: "", length: "", width: "", height: "", sku: "" };
}

/** Variants must all come from the current attribute definition; editing attributes afterwards needs a rebuild. */
export function variantsOutOfSync(attributes: AttributeRow[], variants: VariantValue[]) {
  const titles = new Set(buildVariantTitles(completeAttributes(attributes)));
  return variants.length === 0 || variants.some((variant) => !titles.has(variant.title));
}

const FIELDS: { key: VariantField; label: string; money?: boolean; text?: boolean; placeholder?: string }[] = [
  { key: "price", label: "قیمت", money: true },
  { key: "comparePrice", label: "قیمت خط‌خورده", money: true },
  { key: "stock", label: "تعداد موجودی", placeholder: "نامحدود" },
  { key: "sku", label: "شناسهٔ محصول (SKU)", text: true },
  { key: "minimum", label: "حداقل تعداد در هر سفارش" },
  { key: "maximum", label: "حداکثر تعداد در هر سفارش" },
  { key: "weight", label: "وزن بسته" },
  { key: "length", label: "طول بسته" },
  { key: "width", label: "عرض بسته" },
  { key: "height", label: "ارتفاع بسته" },
];

const ATTRIBUTE_EXAMPLES = [
  ["رنگ", "زرد + آبی"],
  ["اندازه", "کوچک + متوسط + بزرگ"],
  ["حجم", "۱۶ گیگ + ۳۲ گیگ"],
  ["سازنده", "چین + آمریکا"],
];

export function VariantBuilder({ kind, onKindChange, attributes, onAttributes, variants, onVariants }: {
  kind: "simple" | "variable";
  onKindChange: (value: "simple" | "variable") => void;
  attributes: AttributeRow[];
  onAttributes: (value: AttributeRow[]) => void;
  variants: VariantValue[];
  onVariants: (value: VariantValue[]) => void;
}) {
  const [basePrice, setBasePrice] = useState("");
  const [baseComparePrice, setBaseComparePrice] = useState("");
  const [editingIndex, setEditingIndex] = useState<number>();

  const definitions = completeAttributes(attributes);
  const plannedTitles = buildVariantTitles(definitions);
  const outOfSync = kind === "variable" && variants.length > 0 && variantsOutOfSync(attributes, variants);

  function changeKind(value: "simple" | "variable") {
    onKindChange(value);
    if (value === "simple") {
      onVariants([emptyVariant(PRIMARY_TITLE)]);
    } else {
      onVariants([]);
      if (attributes.length === 0) onAttributes([{ name: "", value: "" }]);
    }
  }

  function buildVariants() {
    onVariants(plannedTitles.map((title) =>
      variants.find((variant) => variant.title === title) ?? emptyVariant(title, basePrice, baseComparePrice)));
  }

  return (
    <section className="space-y-5 rounded-xl border border-[var(--border)] bg-white p-5">
      <div className="space-y-3">
        <h2 className="font-black">نوع و واریانت‌ها</h2>
        <div className="flex flex-wrap gap-5 text-sm">
          <label className="flex items-center gap-2"><input type="radio" checked={kind === "simple"} onChange={() => changeKind("simple")} /> کالای ساده</label>
          <label className="flex items-center gap-2"><input type="radio" checked={kind === "variable"} onChange={() => changeKind("variable")} /> کالای چندواریانتی (رنگ، اندازه و…)</label>
        </div>
      </div>

      {kind === "simple" ? (
        <div className="space-y-3 border-t border-[var(--border)] pt-4">
          <VariantFields value={variants[0] ?? emptyVariant(PRIMARY_TITLE)} onChange={(value) => onVariants([value])} />
          <TaxShippingNote />
        </div>
      ) : (
        <>
          <div className="space-y-4 rounded-lg border border-[var(--border)] p-4">
            <h3 className="font-bold">تعریف ویژگی‌ها</h3>
            <p className="rounded-lg bg-[var(--background)] p-3 text-sm leading-7 text-[var(--muted)]">
              نام هر ویژگی و مقدارهایش را مثل نمونه بنویسید و مقدارها را با <strong className="text-[var(--foreground)]">+</strong> از هم جدا کنید؛
              سپس «ساخت واریانت‌ها» را بزنید تا برای هر ترکیب یک واریانت ساخته شود.
            </p>

            <div className="space-y-2">
              {attributes.map((row, index) => (
                <div className="grid grid-cols-[1fr_2fr_auto] gap-2" key={index}>
                  <Input aria-label="نام ویژگی" placeholder={`مانند: ${ATTRIBUTE_EXAMPLES[index % ATTRIBUTE_EXAMPLES.length][0]}`} value={row.name} onChange={(event) => onAttributes(attributes.map((item, current) => current === index ? { ...item, name: event.target.value } : item))} />
                  <Input aria-label="مقدارهای ویژگی" placeholder={`مانند: ${ATTRIBUTE_EXAMPLES[index % ATTRIBUTE_EXAMPLES.length][1]}`} value={row.value} onChange={(event) => onAttributes(attributes.map((item, current) => current === index ? { ...item, value: event.target.value } : item))} />
                  <Button type="button" variant="ghost" aria-label="حذف ویژگی" className="px-3" onClick={() => onAttributes(attributes.filter((_, current) => current !== index))}>×</Button>
                </div>
              ))}
              <Button type="button" variant="secondary" onClick={() => onAttributes([...attributes, { name: "", value: "" }])}>افزودن ویژگی</Button>
            </div>

            <div className="grid gap-3 border-t border-[var(--border)] pt-4 md:grid-cols-2">
              <MoneyInput id="base-price" label="قیمت پایه" value={basePrice} onChange={setBasePrice} />
              <MoneyInput id="base-compare-price" label="قیمت خط‌خورده" value={baseComparePrice} onChange={setBaseComparePrice} />
              <p className="text-xs text-[var(--muted)] md:col-span-2">این قیمت‌ها روی واریانت‌های تازه نشانده می‌شوند؛ بعداً هر واریانت را جداگانه ویرایش کنید.</p>
            </div>

            <div className="flex flex-wrap items-center gap-3">
              <Button type="button" disabled={plannedTitles.length === 0} onClick={buildVariants}>ساخت واریانت‌ها</Button>
              {plannedTitles.length > 0 ? (
                <span className="text-sm text-[var(--muted)]">{formatPersianNumber(plannedTitles.length)} واریانت ساخته می‌شود</span>
              ) : null}
            </div>
          </div>

          {outOfSync ? <Alert tone="warning">ویژگی‌ها پس از ساخت واریانت‌ها تغییر کرده‌اند؛ دوباره «ساخت واریانت‌ها» را بزنید. مقدارهای واریانت‌های قبلی حفظ می‌شوند.</Alert> : null}

          {variants.length > 0 ? (
            <div className="space-y-2">
              <h3 className="font-bold">واریانت‌ها</h3>
              <div className="overflow-x-auto rounded-lg border border-[var(--border)]">
                <table className="w-full text-sm">
                  <thead className="bg-[var(--background)] text-right">
                    <tr>
                      {["#", "عنوان", "قیمت", "قیمت خط‌خورده", "موجودی", "SKU", ""].map((head) => <th key={head} className="whitespace-nowrap px-3 py-2 font-bold">{head}</th>)}
                    </tr>
                  </thead>
                  <tbody>
                    {variants.map((variant, index) => (
                      <tr key={variant.title} className="border-t border-[var(--border)]">
                        <td className="px-3 py-2">{formatPersianNumber(index + 1)}</td>
                        <td className="whitespace-nowrap px-3 py-2">{variant.title}</td>
                        <td className="whitespace-nowrap px-3 py-2">{displayNumber(variant.price)}</td>
                        <td className="whitespace-nowrap px-3 py-2">{displayNumber(variant.comparePrice)}</td>
                        <td className="whitespace-nowrap px-3 py-2">{displayNumber(variant.stock, "نامحدود")}</td>
                        <td className="px-3 py-2">{variant.sku || "—"}</td>
                        <td className="whitespace-nowrap px-3 py-1 text-left">
                          <Button type="button" variant="ghost" className="min-h-8 px-2 text-[var(--primary)]" onClick={() => setEditingIndex(index)}>ویرایش</Button>
                          <Button type="button" variant="ghost" className="min-h-8 px-2" onClick={() => onVariants(variants.filter((_, current) => current !== index))}>حذف</Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <TaxShippingNote />
            </div>
          ) : null}

          {editingIndex != null && variants[editingIndex] ? (
            <VariantDialog
              variant={variants[editingIndex]}
              onSave={(value) => { onVariants(variants.map((item, current) => current === editingIndex ? value : item)); setEditingIndex(undefined); }}
              onClose={() => setEditingIndex(undefined)}
            />
          ) : null}
        </>
      )}
    </section>
  );
}

function TaxShippingNote() {
  return <p className="text-xs text-[var(--muted)]">مالیات و هزینهٔ ارسال بر اساس تنظیمات فروشگاه محاسبه می‌شوند.</p>;
}

function VariantFields({ value, onChange }: { value: VariantValue; onChange: (value: VariantValue) => void }) {
  return (
    <div className="grid gap-4 md:grid-cols-2">
      {FIELDS.map((field) => field.money ? (
        <MoneyInput key={field.key} id={`variant-${field.key}`} label={field.label} value={value[field.key]} onChange={(next) => onChange({ ...value, [field.key]: next })} />
      ) : (
        <div className="space-y-2" key={field.key}>
          <label className="text-sm font-bold" htmlFor={`variant-${field.key}`}>{field.label}</label>
          <Input
            id={`variant-${field.key}`}
            inputMode={field.text ? undefined : "numeric"}
            dir={field.text ? undefined : "ltr"}
            className={field.text ? "" : "text-right"}
            placeholder={field.placeholder}
            value={value[field.key]}
            onChange={(event) => onChange({ ...value, [field.key]: event.target.value })}
          />
        </div>
      ))}
    </div>
  );
}

function MoneyInput({ id, label, value, onChange }: { id: string; label: string; value: string; onChange: (value: string) => void }) {
  const preview = displayNumber(value, "");
  return (
    <div className="space-y-2">
      <label className="flex items-baseline justify-between gap-2 text-sm font-bold" htmlFor={id}>
        {label}
        {preview ? <span className="text-xs font-normal text-[var(--muted)]">{preview} تومان</span> : null}
      </label>
      <div className="flex">
        <Input id={id} inputMode="decimal" dir="ltr" className="rounded-e-none text-right" value={value} onChange={(event) => onChange(event.target.value)} />
        <span className="flex items-center rounded-e-lg border border-s-0 border-[var(--border)] bg-[var(--background)] px-3 text-sm text-[var(--muted)]">تومان</span>
      </div>
    </div>
  );
}

function VariantDialog({ variant, onSave, onClose }: { variant: VariantValue; onSave: (value: VariantValue) => void; onClose: () => void }) {
  const ref = useRef<HTMLDialogElement>(null);
  const [draft, setDraft] = useState(variant);

  useEffect(() => {
    ref.current?.showModal();
  }, []);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    // React bubbles submit through the portal to the product form; stop it there.
    event.stopPropagation();
    onSave(draft);
  }

  // Portalled to <body> so the dialog's inputs are not part of the product form's native submission.
  return createPortal(
    <dialog ref={ref} dir="rtl" onClose={onClose} className="m-auto w-[min(48rem,calc(100vw-2rem))] rounded-xl border border-[var(--border)] bg-white p-0 text-[var(--foreground)] shadow-xl backdrop:bg-black/40">
      <form onSubmit={handleSubmit}>
        <header className="flex items-center justify-between border-b border-[var(--border)] px-5 py-4">
          <h2 className="font-black">ویرایش {variant.title}</h2>
          <button type="button" aria-label="بستن" className="text-xl leading-none text-[var(--muted)] hover:text-[var(--foreground)]" onClick={() => ref.current?.close()}>×</button>
        </header>
        <div className="max-h-[70vh] space-y-3 overflow-y-auto p-5">
          <VariantFields value={draft} onChange={setDraft} />
          <TaxShippingNote />
        </div>
        <footer className="flex gap-2 border-t border-[var(--border)] px-5 py-4">
          <Button type="submit">اعمال تغییرات</Button>
          <Button type="button" variant="secondary" onClick={() => ref.current?.close()}>انصراف</Button>
        </footer>
      </form>
    </dialog>,
    document.body,
  );
}

function displayNumber(raw: string, empty = "—") {
  const normalized = normalizeNumericInput(raw);
  if (typeof normalized !== "string" || normalized === "") return empty;
  const number = Number(normalized);
  return Number.isFinite(number) ? formatPersianNumber(number) : raw;
}
