// "Before → after" per field for one AuditLog record (ADR-014, view 3), never
// raw JSON. Pure, so it is tested with node --test.

export type DiffValue =
  | { kind: "empty" }
  | { kind: "scalar"; text: string }
  | { kind: "html"; text: string }
  | { kind: "collection"; summary: string; detail: string };

export interface DiffRow {
  field: string;
  before: DiffValue;
  after: DiffValue;
  /** "created": no before snapshot at all (e.g. a create record) — only the new value is meaningful. */
  kind: "changed" | "created";
}

// Rich-text fields of a portal product: shown as source text, never rendered.
const htmlFields = new Set(["description", "Description"]);

const itemCount = new Intl.NumberFormat("fa-IR");

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

export function describeValue(field: string, value: unknown): DiffValue {
  if (value === null || value === undefined || value === "") {
    return { kind: "empty" };
  }
  if (Array.isArray(value)) {
    return value.length === 0
      ? { kind: "empty" }
      : { kind: "collection", summary: `${itemCount.format(value.length)} مورد`, detail: JSON.stringify(value, null, 2) };
  }
  if (isPlainObject(value)) {
    const size = Object.keys(value).length;
    return size === 0
      ? { kind: "empty" }
      : { kind: "collection", summary: `${itemCount.format(size)} فیلد`, detail: JSON.stringify(value, null, 2) };
  }
  if (typeof value === "string" && htmlFields.has(field)) {
    return { kind: "html", text: value };
  }
  if (typeof value === "boolean") {
    return { kind: "scalar", text: value ? "بله" : "خیر" };
  }
  return { kind: "scalar", text: String(value) };
}

function asFields(snapshot: unknown): Record<string, unknown> | null {
  return isPlainObject(snapshot) ? snapshot : null;
}

export function computeAuditDiff(before: unknown, after: unknown, changedFields: unknown): DiffRow[] {
  const beforeFields = asFields(before);
  const afterFields = asFields(after);
  const changed = Array.isArray(changedFields) ? changedFields.filter((name): name is string => typeof name === "string") : [];

  if (changed.length > 0) {
    return changed.map((field) => ({
      field,
      before: describeValue(field, beforeFields?.[field]),
      after: describeValue(field, afterFields?.[field]),
      kind: beforeFields ? "changed" : "created",
    }));
  }

  // No ChangedFields: a create record (only After), or a record that never had
  // a Before to compare against — show every After field as a new value.
  if (!beforeFields && afterFields) {
    return Object.entries(afterFields).map(([field, value]) => ({
      field,
      before: { kind: "empty" },
      after: describeValue(field, value),
      kind: "created",
    }));
  }

  return [];
}
