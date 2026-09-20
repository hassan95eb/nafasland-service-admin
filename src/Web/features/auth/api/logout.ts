import { apiFetch } from "@/shared/lib/api-client";

export function logout() {
  return apiFetch<void>("/api/v1/identity/auth/logout", { method: "POST" });
}
