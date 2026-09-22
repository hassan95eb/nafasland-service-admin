"use client";

import Image from "@tiptap/extension-image";
import { TableKit } from "@tiptap/extension-table";
import TextAlign from "@tiptap/extension-text-align";
import { EditorContent, useEditor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import { useEffect, useMemo } from "react";

import type { RichTextState } from "@/features/products/lib/rich-text-state";
import { changeRichText, switchRichTextMode } from "@/features/products/lib/rich-text-state";
import { findUnsupportedHtml, sanitizeProductHtml } from "@/shared/lib/sanitize-html";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";

export function RichTextEditor({
  id,
  label,
  state,
  onChange,
}: {
  id: string;
  label: string;
  state: RichTextState;
  onChange: (state: RichTextState) => void;
}) {
  const warnings = useMemo(() => findUnsupportedHtml(state.original), [state.original]);
  const editor = useEditor({
    immediatelyRender: false,
    extensions: [
      StarterKit.configure({ heading: { levels: [2, 3, 4] } }),
      Image.configure({ allowBase64: false }),
      TableKit,
      TextAlign.configure({ types: ["heading", "paragraph"] }),
    ],
    content: sanitizeProductHtml(state.value),
    onUpdate: ({ editor: currentEditor }) => onChange(changeRichText(state, currentEditor.getHTML())),
  });

  useEffect(() => {
    if (state.mode === "visual" && editor && editor.getHTML() !== sanitizeProductHtml(state.value)) {
      editor.commands.setContent(sanitizeProductHtml(state.value), { emitUpdate: false });
    }
  }, [editor, state.mode, state.value]);

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <label className="text-sm font-bold" htmlFor={id}>{label}</label>
        <div className="flex gap-2">
          <Button type="button" variant={state.mode === "visual" ? "primary" : "secondary"} onClick={() => onChange(switchRichTextMode(state, "visual"))}>
            ویرایشگر
          </Button>
          <Button type="button" variant={state.mode === "html" ? "primary" : "secondary"} onClick={() => onChange(switchRichTextMode(state, "html"))}>
            کد HTML
          </Button>
        </div>
      </div>
      {warnings.length > 0 ? (
        <Alert tone="warning">
          {warnings[0]} برای جلوگیری از حذف بی‌صدا، متن اصلی از نمای کد HTML قابل ویرایش است.
        </Alert>
      ) : null}
      {state.mode === "html" ? (
        <textarea
          id={id}
          className="min-h-52 w-full rounded-lg border border-[var(--border)] bg-white p-3 font-mono text-sm"
          dir="ltr"
          value={state.value}
          onChange={(event) => onChange(changeRichText(state, event.target.value))}
        />
      ) : (
        <div id={id} className="min-h-52 rounded-lg border border-[var(--border)] bg-white p-3 [&_.tiptap]:min-h-44">
          <EditorContent editor={editor} />
        </div>
      )}
    </div>
  );
}
