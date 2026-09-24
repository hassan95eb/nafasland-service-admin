import { z } from "zod";

import { normalizeNumericInput } from "../../../shared/lib/normalize-number.ts";

const optionalNumber = z.preprocess(
  (value) => value === "" || value == null ? null : normalizeNumericInput(String(value)),
  z.union([z.null(), z.coerce.number().nonnegative()]),
);

const optionalInteger = z.preprocess(
  (value) => value === "" || value == null ? null : normalizeNumericInput(String(value)),
  z.union([z.null(), z.coerce.number().int("مقدار باید عدد صحیح باشد.").nonnegative()]),
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
  comparePrice: optionalNumber,
  stock: optionalInteger,
  minimum: optionalInteger,
  maximum: optionalInteger,
  weight: optionalNumber,
  length: optionalNumber,
  width: optionalNumber,
  height: optionalNumber,
  sku: z.string(),
});

/** Values are typed the way the NafasLand panel asks for them: «زرد + ابی». Comma separators are still accepted. */
export function parseAttributeValues(raw: string) {
  return [...new Set(raw.split(/[+،,]/).map((value) => value.trim()).filter(Boolean))];
}

export type AttributeDefinition = { name: string; values: string[] };

export function completeAttributes(rows: { name: string; value: string }[]): AttributeDefinition[] {
  return rows
    .map((row) => ({ name: row.name.trim(), values: parseAttributeValues(row.value) }))
    .filter((row) => row.name && row.values.length > 0);
}

/** One variant per combination, titled like the NafasLand panel: «رنگ: زرد، اندازه: کوچک». */
export function buildVariantTitles(attributes: AttributeDefinition[]) {
  if (attributes.length === 0) return [];
  return attributes
    .reduce<string[][]>(
      (combinations, attribute) => combinations.flatMap((combination) =>
        attribute.values.map((value) => [...combination, `${attribute.name}: ${value}`])),
      [[]],
    )
    .map((parts) => parts.join("، "));
}

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
