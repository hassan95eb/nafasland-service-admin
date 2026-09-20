import { formatPersianNumber } from "@/shared/lib/formatters";
import { Button } from "@/shared/ui/button";

export function Pagination({
  page,
  pageCount,
  onPageChange,
  disabled,
}: {
  page: number;
  pageCount: number;
  onPageChange: (page: number) => void;
  disabled?: boolean;
}) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-t border-[var(--border)] px-4 py-3">
      <p className="text-sm text-[var(--muted)]">
        صفحهٔ {formatPersianNumber(page)} از {formatPersianNumber(Math.max(pageCount, 1))}
      </p>
      <div className="flex gap-2">
        <Button
          variant="secondary"
          disabled={disabled || page <= 1}
          onClick={() => onPageChange(page - 1)}
        >
          قبلی
        </Button>
        <Button
          variant="secondary"
          disabled={disabled || page >= pageCount}
          onClick={() => onPageChange(page + 1)}
        >
          بعدی
        </Button>
      </div>
    </div>
  );
}
