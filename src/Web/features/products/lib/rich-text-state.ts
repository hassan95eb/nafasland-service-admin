export interface RichTextState {
  original: string;
  value: string;
  dirty: boolean;
  mode: "visual" | "html";
}

export function createRichTextState(value: string | null | undefined): RichTextState {
  const original = value ?? "";
  return { original, value: original, dirty: false, mode: "visual" };
}

export function changeRichText(state: RichTextState, value: string): RichTextState {
  return { ...state, value, dirty: true };
}

export function switchRichTextMode(state: RichTextState, mode: RichTextState["mode"]): RichTextState {
  return { ...state, mode };
}

/**
 * A link or image address typed into the editor: absolute http(s) or
 * site-relative ("/uploads/…"). A bare "example.com" gets https://; anything
 * else (javascript:, data:, mailto:, …) is rejected — ADR-033 bans script links.
 */
export function normalizeEditorUrl(input: string) {
  const value = input.trim();
  if (!value || /\s/.test(value) || value.startsWith("//")) return undefined;
  if (value.startsWith("/")) return value;
  const candidate = /^[a-z][a-z0-9+.-]*:/i.test(value) ? value : `https://${value}`;
  try {
    const url = new URL(candidate);
    return url.protocol === "http:" || url.protocol === "https:" ? candidate : undefined;
  } catch {
    return undefined;
  }
}
