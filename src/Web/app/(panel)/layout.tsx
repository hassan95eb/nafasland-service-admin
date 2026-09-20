import { redirect } from "next/navigation";
import type { ReactNode } from "react";

import { getCurrentUser } from "@/shared/lib/server-api";
import { PermissionProvider } from "@/shared/permissions/permission-context";
import { Sidebar } from "@/shared/permissions/sidebar";
import { Alert } from "@/shared/ui/alert";

const productsReadPermission = "catalog.products.read";

export default async function PanelLayout({ children }: Readonly<{ children: ReactNode }>) {
  const user = await getCurrentUser();
  if (!user) {
    redirect("/login");
  }

  if (!user.permissions.includes(productsReadPermission)) {
    redirect("/forbidden");
  }

  return (
    <PermissionProvider permissions={user.permissions}>
      <div className="min-h-screen lg:flex">
        <Sidebar username={user.username} permissions={user.permissions} />
        <main className="min-w-0 flex-1 p-4 sm:p-6 lg:p-8">
          {user.mustChangePassword ? (
            <div className="mb-5">
              <Alert tone="warning">رمز عبور باید تغییر کند. امکان تغییر رمز در مرحلهٔ بعد تکمیل می‌شود.</Alert>
            </div>
          ) : null}
          {children}
        </main>
      </div>
    </PermissionProvider>
  );
}
