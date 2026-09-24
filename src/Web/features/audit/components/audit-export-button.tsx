"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";

import { auditExportDownloadUrl, auditQueryKeys, startAuditExport, type ExportFormat } from "@/features/audit/api/audit-api";
import { useAuditExportStatus } from "@/features/audit/hooks/use-audit";
import type { AuditFilters } from "@/features/audit/lib/audit-filters";
import { presentApiError } from "@/shared/lib/api-client";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";

// A background export (> 25,000 rows) survives leaving the page: its job id is
// remembered here. Storage can throw (private mode, blocked site data) — then
// the job simply is not resumed, nothing breaks.
const pendingJobStorageKey = "nafasland-audit-export-job";

function readPendingJob() {
  try {
    return window.localStorage.getItem(pendingJobStorageKey);
  } catch {
    return null;
  }
}

function writePendingJob(jobId: string | null) {
  try {
    if (jobId) {
      window.localStorage.setItem(pendingJobStorageKey, jobId);
    } else {
      window.localStorage.removeItem(pendingJobStorageKey);
    }
  } catch {
    // See above: best-effort only.
  }
}

function saveBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.append(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

/** Sends exactly the filters active in the view (ADR-014); the backend applies the same AuditLogFilter as the list. */
export function AuditExportButton({ filters }: { filters: AuditFilters }) {
  const queryClient = useQueryClient();
  const [format, setFormat] = useState<ExportFormat>("xlsx");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const [jobId, setJobId] = useState<string | null>(null);
  const status = useAuditExportStatus(jobId);

  useEffect(() => {
    // Resume a background export started before the user left the page.
    // eslint-disable-next-line react-hooks/set-state-in-effect -- localStorage is only readable after mount.
    setJobId(readPendingJob());
  }, []);

  async function exportNow() {
    setBusy(true);
    setError(undefined);
    try {
      const result = await startAuditExport(filters, format);
      if (result.kind === "file") {
        saveBlob(result.blob, result.fileName);
      } else {
        writePendingJob(result.jobId);
        setJobId(result.jobId);
      }
      // The export itself is now an AuditExported row in the log.
      void queryClient.invalidateQueries({ queryKey: [...auditQueryKeys.all, "logs"] });
    } catch (exportError) {
      setError(presentApiError(exportError));
    } finally {
      setBusy(false);
    }
  }

  function dismissJob() {
    writePendingJob(null);
    setJobId(null);
  }

  const job = status.data;

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center gap-2">
        <select
          className="min-h-10 rounded-lg border border-[var(--border)] bg-white px-3 text-sm"
          value={format}
          aria-label="قالب خروجی"
          onChange={(event) => setFormat(event.target.value as ExportFormat)}
        >
          <option value="xlsx">Excel</option>
          <option value="csv">CSV</option>
        </select>
        <Button type="button" variant="secondary" loading={busy} disabled={Boolean(jobId) && job?.status !== "Completed" && job?.status !== "Failed"} onClick={exportNow}>
          خروجی با فیلترهای فعلی
        </Button>
      </div>

      {error ? <Alert>{error}</Alert> : null}

      {jobId ? (
        <div className="flex flex-wrap items-center gap-3 rounded-lg bg-neutral-50 px-4 py-3 text-sm">
          {status.error ? (
            <span className="text-[var(--danger)]">{presentApiError(status.error)}</span>
          ) : job?.status === "Completed" ? (
            <>
              <span>فایل خروجی آماده است.</span>
              <a className="font-bold text-[var(--primary)] hover:underline" href={auditExportDownloadUrl(jobId)}>دریافت فایل</a>
            </>
          ) : job?.status === "Failed" ? (
            <span className="text-[var(--danger)]">آماده‌سازی فایل ناموفق بود. دوباره تلاش کنید.</span>
          ) : (
            <span>تعداد ردیف‌ها زیاد است؛ فایل در حال آماده‌سازی است و وضعیتش هر چند ثانیه بررسی می‌شود.</span>
          )}
          <Button type="button" variant="ghost" className="min-h-8 px-3 py-1 text-xs" onClick={dismissJob}>بستن</Button>
        </div>
      ) : null}
    </div>
  );
}
