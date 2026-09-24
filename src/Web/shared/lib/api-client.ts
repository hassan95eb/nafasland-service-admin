const antiforgeryCookieName = "nafasland-admin-xsrf";

export interface ProblemDetails {
  title?: string;
  detail?: string;
  correlationId?: string;
}

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly correlationId?: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

function readCookie(name: string) {
  if (typeof document === "undefined") {
    return undefined;
  }

  const encodedName = `${encodeURIComponent(name)}=`;
  const value = document.cookie
    .split("; ")
    .find((part) => part.startsWith(encodedName))
    ?.slice(encodedName.length);

  return value ? decodeURIComponent(value) : undefined;
}

export function storeAntiforgeryToken(token: string) {
  const secure = window.location.protocol === "https:" ? "; Secure" : "";
  document.cookie = `${encodeURIComponent(antiforgeryCookieName)}=${encodeURIComponent(token)}; Path=/; SameSite=Strict${secure}`;
}

export function clearAntiforgeryToken() {
  document.cookie = `${encodeURIComponent(antiforgeryCookieName)}=; Path=/; Max-Age=0; SameSite=Strict`;
}

function isMutation(method: string) {
  return !["GET", "HEAD", "OPTIONS"].includes(method.toUpperCase());
}

/** Same headers, antiforgery and error handling as apiFetch, for a caller that needs the raw response (e.g. a file download). */
export async function apiFetchResponse(path: string, init: RequestInit = {}): Promise<Response> {
  const method = init.method ?? "GET";
  const headers = new Headers(init.headers);

  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  if (isMutation(method)) {
    const token = readCookie(antiforgeryCookieName);
    if (token) {
      headers.set("X-XSRF-TOKEN", token);
    }
  }

  const response = await fetch(path, {
    ...init,
    method,
    headers,
    credentials: "same-origin",
  });

  if (!response.ok) {
    let problem: ProblemDetails = {};
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      // A non-JSON proxy failure is still represented by a stable Persian message.
    }

    const message =
      problem.detail ?? problem.title ??
      (response.status === 401 ? "نام کاربری یا رمز عبور درست نیست." : "در ارتباط با سرور مشکلی پیش آمد.");
    throw new ApiError(message, response.status, problem.correlationId);
  }

  return response;
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await apiFetchResponse(path, init);

  if (response.status === 204 || response.headers.get("content-length") === "0") {
    return undefined as T;
  }

  const text = await response.text();
  return text ? (JSON.parse(text) as T) : (undefined as T);
}

export function presentApiError(error: unknown) {
  if (!(error instanceof ApiError)) {
    return "خطای پیش‌بینی‌نشده‌ای رخ داد. دوباره تلاش کنید.";
  }

  return error.correlationId
    ? `${error.message} کد پیگیری: ${error.correlationId}`
    : error.message;
}
