import type { components } from "@/shared/lib/api-types.generated";
import { apiFetch } from "@/shared/lib/api-client";

type ChangePasswordRequest = components["schemas"]["ChangePasswordRequest"];

export function changePassword(input: ChangePasswordRequest) {
  return apiFetch<void>("/api/v1/identity/auth/change-password", {
    method: "POST",
    body: JSON.stringify(input),
  });
}
