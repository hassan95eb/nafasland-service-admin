import Link from "next/link";

export default function ForbiddenPage() {
  return (
    <main className="grid min-h-screen place-items-center px-4">
      <section className="max-w-lg rounded-2xl border border-[var(--border)] bg-white p-8 text-center shadow-sm">
        <p className="text-sm font-bold text-red-700">دسترسی غیرمجاز</p>
        <h1 className="mt-2 text-2xl font-black">اجازهٔ مشاهدهٔ این بخش را ندارید</h1>
        <p className="mt-3 text-sm leading-7 text-[var(--muted)]">برای دریافت permission لازم با مدیر سامانه تماس بگیرید.</p>
        <Link className="mt-6 inline-block text-sm font-bold text-[var(--primary)]" href="/products">
          بازگشت به پنل
        </Link>
      </section>
    </main>
  );
}
