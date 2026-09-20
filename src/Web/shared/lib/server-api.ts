import { cookies } from "next/headers";

import type { components } from "@/shared/lib/api-types.generated";

type MeResponse = components["schemas"]["MeResponse"];

export async function getCurrentUser(): Promise<MeResponse | null> {
  const requestCookies = await cookies();
  const cookieHeader = requestCookies
    .getAll()
    .map(({ name, value }) => `${name}=${value}`)
    .join("; ");
  const baseUrl = process.env.INTERNAL_API_BASE_URL ?? "http://api:8080";

  try {
    const response = await fetch(`${baseUrl}/api/v1/identity/auth/me`, {
      headers: cookieHeader ? { Cookie: cookieHeader } : undefined,
      cache: "no-store",
    });

    if (!response.ok) {
      return null;
    }

    return (await response.json()) as MeResponse;
  } catch {
    return null;
  }
}
