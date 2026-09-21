import { z } from "zod";

import { normalizeNumericInput } from "@/shared/lib/normalize-number";

const normalizedNumber = z.preprocess(normalizeNumericInput, z.coerce.number());

export const updateVariantSchema = z.object({
  newPrice: normalizedNumber.pipe(z.number().nonnegative("قیمت نمی‌تواند منفی باشد.")),
  newStock: normalizedNumber.pipe(z.number().int("موجودی باید عدد صحیح باشد.").nonnegative("موجودی نمی‌تواند منفی باشد.")),
});
