"use client";

import { usePathname } from "next/navigation";

import { ChangePasswordForm } from "@/features/auth/components/change-password-form";
import { LogoutButton } from "@/features/auth/components/logout-button";

export function ForcedPasswordChange() {
  const pathname = usePathname();
  const returnTo = pathname === "/change-password" ? "/products" : pathname;

  return (
    <main className="grid min-h-screen place-items-center px-4 py-10">
      <section className="w-full max-w-md rounded-2xl border border-[var(--border)] bg-white p-6 shadow-sm sm:p-8">
        <div className="mb-7 space-y-2 text-center">
          <p className="text-sm font-bold text-[var(--primary)]">اقدام الزامی</p>
          <h1 className="text-2xl font-black">رمز عبور را تغییر دهید</h1>
          <p className="text-sm leading-7 text-[var(--muted)]">تا پیش از تعیین رمز تازه، بخش دیگری از پنل در دسترس نیست.</p>
        </div>
        <ChangePasswordForm returnTo={returnTo} />
        <div className="mt-4"><LogoutButton /></div>
      </section>
    </main>
  );
}
