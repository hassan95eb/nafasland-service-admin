import { Spinner } from "@/shared/ui/spinner";

export function LoadingOverlay({ label = "در حال ذخیره…" }: { label?: string }) {
  return (
    <div className="absolute inset-0 z-10 flex items-center justify-center gap-2 rounded-xl bg-white/80 text-sm font-bold text-[var(--primary-strong)] backdrop-blur-sm">
      <Spinner className="size-5" />
      {label}
    </div>
  );
}
