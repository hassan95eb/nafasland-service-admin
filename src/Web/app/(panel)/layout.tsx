import { redirect } from "next/navigation";
import type { ReactNode } from "react";

import { getCurrentUser } from "@/shared/lib/server-api";
import { PanelShell } from "@/shared/permissions/panel-shell";

export default async function PanelLayout({ children }: Readonly<{ children: ReactNode }>) {
  const user = await getCurrentUser();
  if (!user) {
    redirect("/login");
  }

  return (
    <PanelShell username={user.username} permissions={user.permissions} mustChangePassword={user.mustChangePassword}>
      {children}
    </PanelShell>
  );
}
