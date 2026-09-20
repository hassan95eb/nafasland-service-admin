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
