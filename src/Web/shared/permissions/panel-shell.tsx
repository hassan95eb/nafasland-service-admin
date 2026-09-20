"use client";

import type { ReactNode } from "react";

import { ForcedPasswordChange } from "@/features/auth/components/forced-password-change";
import { PermissionProvider } from "@/shared/permissions/permission-context";
import { Sidebar } from "@/shared/permissions/sidebar";

export function PanelShell({
  username,
  permissions,
  mustChangePassword,
  children,
}: {
  username: string;
  permissions: string[];
  mustChangePassword: boolean;
  children: ReactNode;
}) {
  if (mustChangePassword) {
    return <ForcedPasswordChange />;
  }

  return (
    <PermissionProvider permissions={permissions}>
      <div className="min-h-screen lg:flex">
        <Sidebar username={username} permissions={permissions} />
        <main className="min-w-0 flex-1 p-4 sm:p-6 lg:p-8">{children}</main>
      </div>
    </PermissionProvider>
  );
}
