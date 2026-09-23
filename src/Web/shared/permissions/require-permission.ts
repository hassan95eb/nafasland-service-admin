import { redirect } from "next/navigation";

import { getCurrentUser } from "@/shared/lib/server-api";

export async function requirePermission(permission: string) {
  const user = await getCurrentUser();
  if (!user) {
    redirect("/login");
  }
  if (user.mustChangePassword) {
    return user;
  }
  if (!user.permissions.includes(permission)) {
    redirect("/forbidden");
  }
  return user;
}

/**
 * The frontend counterpart of the backend's IRequiresAuthenticatedUser marker:
 * signed in, no specific permission required. Used by pages like "my approval
 * requests" (ADR-010's "approval.read.own", available to every role) where
 * requirePermission's own permission check would have nothing to check against.
 */
export async function requireAuthenticatedUser() {
  const user = await getCurrentUser();
  if (!user) {
    redirect("/login");
  }
  return user;
}
