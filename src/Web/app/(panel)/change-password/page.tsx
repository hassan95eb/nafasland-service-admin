import { ChangePasswordForm } from "@/features/auth/components/change-password-form";

export default function ChangePasswordPage() {
  return (
    <section className="mx-auto max-w-md rounded-2xl border border-[var(--border)] bg-white p-6 shadow-sm sm:p-8">
      <div className="mb-7 space-y-2 text-center">
        <h1 className="text-2xl font-black">تغییر رمز عبور</h1>
        <p className="text-sm text-[var(--muted)]">رمز فعلی و رمز تازه را وارد کنید.</p>
      </div>
      <ChangePasswordForm />
    </section>
  );
}
