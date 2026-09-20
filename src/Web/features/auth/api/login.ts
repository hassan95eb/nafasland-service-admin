import type { components } from "@/shared/lib/api-types.generated";
import { apiFetch } from "@/shared/lib/api-client";

type LoginRequest = components["schemas"]["LoginRequest"];
type LoginResponse = components["schemas"]["LoginResult"];

export function login(request: LoginRequest) {
  return apiFetch<LoginResponse>("/api/v1/identity/auth/login", {
    method: "POST",
    body: JSON.stringify(request),
  });
}
