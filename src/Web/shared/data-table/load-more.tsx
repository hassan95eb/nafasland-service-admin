import { Button } from "@/shared/ui/button";

/**
 * Keyset ("cursor") paging: there is no page count or page number to show,
 * only whether the server returned a nextCursor. Pagination (next to this file)
 * stays the offset-based control used by the product list.
 */
export function LoadMore({
  hasMore,
  loading,
  onLoadMore,
}: {
  hasMore: boolean;
  loading: boolean;
  onLoadMore: () => void;
}) {
  if (!hasMore) {
    return null;
  }

  return (
    <div className="flex justify-center border-t border-[var(--border)] px-4 py-3">
      <Button type="button" variant="secondary" loading={loading} onClick={onLoadMore}>
        {loading ? "در حال دریافت…" : "بارگذاری بیشتر"}
      </Button>
    </div>
  );
}
