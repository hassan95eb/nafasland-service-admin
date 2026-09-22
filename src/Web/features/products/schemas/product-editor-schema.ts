import { z } from "zod";

import { normalizeNumericInput } from "../../../shared/lib/normalize-number.ts";

const optionalNumber = z.preprocess(
  (value) => value === "" || value == null ? null : normalizeNumericInput(String(value)),
  z.union([z.coerce.number().nonnegative(), z.null()]),
);

export const productEditorSchema = z.object({
  title: z.string().trim().min(1, "عنوان محصول الزامی است.").max(500),
  caption: z.string(),
  slug: z.string(),
  metaTitle: z.string(),
  metaDescription: z.string(),
  metaKeywords: z.string(),
  metaRobots: z.string(),
  redirect: z.string(),
});

export const createVariantSchema = z.object({
  title: z.string().trim().min(1, "عنوان واریانت الزامی است."),
  price: optionalNumber,
  stock: optionalNumber,
  sku: z.string(),
});

export function validateProductKind(kind: "simple" | "variable", variantTitles: string[]) {
  if (kind === "simple") {
    return variantTitles.length === 1 && variantTitles[0] === "primary"
      ? undefined
      : "کالای ساده باید دقیقاً یک واریانت primary داشته باشد.";
  }
  return variantTitles.length > 0 && variantTitles.every(Boolean)
    ? undefined
    : "کالای چندواریانتی حداقل یک واریانت با عنوان صریح لازم دارد.";
}

export function isSelectableCategory(type: string | null) {
  return type === "store";
}

export function selectedFilterIds(values: ReadonlySet<string>) {
  return [...values].map(Number);
}
