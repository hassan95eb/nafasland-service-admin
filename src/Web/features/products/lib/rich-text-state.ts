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
