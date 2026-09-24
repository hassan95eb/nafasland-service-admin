import assert from "node:assert/strict";
import test from "node:test";

import { parseAuditFilters, serializeAuditFilters, toApiFilters } from "../features/audit/lib/audit-filters.ts";

test("فیلترها از URL خوانده و دوباره نوشته می‌شوند؛ مقدار نامعتبر کنار گذاشته می‌شود", () => {
  const filters = parseAuditFilters(new URLSearchParams("outcome=Denied&action=ProductUpdated&from=1405-06-22&to=bad&extra=1"));

  assert.deepEqual(filters, { outcome: "Denied", action: "ProductUpdated", from: "1405-06-22" });
  assert.equal(serializeAuditFilters(filters).toString(), "from=1405-06-22&action=ProductUpdated&outcome=Denied");
  assert.deepEqual(parseAuditFilters(new URLSearchParams("outcome=Maybe")), {});
});

test("فیلترهای API دقیقاً همان فیلترهای فعال‌اند و بقیه null", () => {
  assert.deepEqual(toApiFilters({ outcome: "Failed", to: "1405-06-22" }), {
    actorUserId: null,
    from: null,
    to: "2026-09-13T20:29:59.999Z",
    action: null,
    entityType: null,
    outcome: "Failed",
  });
});
