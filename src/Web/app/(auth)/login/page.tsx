import { LoginForm } from "@/features/auth/components/login-form";

export default function LoginPage() {
  return (
    <main className="grid min-h-screen place-items-center px-4 py-10">
      <section className="w-full max-w-md rounded-2xl border border-[var(--border)] bg-white p-6 shadow-sm sm:p-8">
        <div className="mb-7 space-y-2 text-center">
          <p className="text-sm font-bold text-[var(--primary)]">نفس‌لند</p>
          <h1 className="text-2xl font-black">ورود به پنل مدیریت</h1>
          <p className="text-sm leading-7 text-[var(--muted)]">با حساب سازمانی خود وارد شوید.</p>
        </div>
        <LoginForm />
      </section>
    </main>
  );
}
