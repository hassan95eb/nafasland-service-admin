"use client";

import Image from "@tiptap/extension-image";
import { TableKit } from "@tiptap/extension-table";
import TextAlign from "@tiptap/extension-text-align";
import { EditorContent, useEditor, useEditorState, type Editor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";

import type { RichTextState } from "@/features/products/lib/rich-text-state";
import { changeRichText, normalizeEditorUrl, switchRichTextMode } from "@/features/products/lib/rich-text-state";
import { findUnsupportedHtml, sanitizeProductHtml } from "@/shared/lib/sanitize-html";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";

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

  // useEditor keeps the callbacks of its first render; reading the latest
  // state/onChange through a ref stops an edit from being written back onto a
  // stale copy of the form (e.g. undoing a section title typed afterwards).
  const latest = useRef({ state, onChange });
  useEffect(() => {
    latest.current = { state, onChange };
  });

  const editor = useEditor({
    immediatelyRender: false,
    extensions: [
      StarterKit.configure({
        heading: { levels: [2, 3, 4] },
        // Only what the ADR-033 allowlist keeps: these produce tags
        // (<code>, <pre>, <blockquote>, <hr>, <s>) the sanitizers would drop.
        code: false,
        codeBlock: false,
        blockquote: false,
        horizontalRule: false,
        strike: false,
        link: {
          openOnClick: false,
          autolink: true,
          defaultProtocol: "https",
          // target/rel are outside the attribute allowlist (ADR-033).
          HTMLAttributes: { target: null, rel: null },
        },
      }),
      Image.configure({ allowBase64: false }),
      TableKit,
      TextAlign.configure({ types: ["heading", "paragraph"] }),
    ],
    content: sanitizeProductHtml(state.value),
    onUpdate: ({ editor: currentEditor }) => {
      const { state: current, onChange: notify } = latest.current;
      notify(changeRichText(current, currentEditor.getHTML()));
    },
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
        <div className="overflow-hidden rounded-lg border border-[var(--border)] bg-white">
          {editor ? <Toolbar editor={editor} /> : null}
          <div
            id={id}
            className="min-h-52 p-3 [&_.tiptap]:min-h-44 [&_.tiptap]:outline-none [&_.tiptap_a]:text-[var(--primary)] [&_.tiptap_a]:underline [&_.tiptap_h2]:text-xl [&_.tiptap_h2]:font-black [&_.tiptap_h3]:text-lg [&_.tiptap_h3]:font-bold [&_.tiptap_h4]:font-bold [&_.tiptap_img]:max-h-64 [&_.tiptap_ol]:list-decimal [&_.tiptap_ol]:pr-6 [&_.tiptap_td]:border [&_.tiptap_td]:p-2 [&_.tiptap_th]:border [&_.tiptap_th]:p-2 [&_.tiptap_ul]:list-disc [&_.tiptap_ul]:pr-6"
          >
            <EditorContent editor={editor} />
          </div>
        </div>
      )}
    </div>
  );
}

type Panel = "link" | "image" | null;

function Toolbar({ editor }: { editor: Editor }) {
  const [panel, setPanel] = useState<Panel>(null);
  const active = useEditorState({
    editor,
    selector: ({ editor: current }) => ({
      bold: current.isActive("bold"),
      italic: current.isActive("italic"),
      underline: current.isActive("underline"),
      h2: current.isActive("heading", { level: 2 }),
      h3: current.isActive("heading", { level: 3 }),
      h4: current.isActive("heading", { level: 4 }),
      bulletList: current.isActive("bulletList"),
      orderedList: current.isActive("orderedList"),
      right: current.isActive({ textAlign: "right" }),
      center: current.isActive({ textAlign: "center" }),
      left: current.isActive({ textAlign: "left" }),
      justify: current.isActive({ textAlign: "justify" }),
      link: current.isActive("link"),
      table: current.isActive("table"),
    }),
  });
  const chain = () => editor.chain().focus();
  const togglePanel = (next: Exclude<Panel, null>) => setPanel(panel === next ? null : next);

  return (
    <div className="border-b border-[var(--border)] bg-neutral-50">
      <div className="flex flex-wrap items-center gap-1 p-1.5" role="toolbar" aria-label="ابزار قالب‌بندی">
        <ToolButton label="پررنگ" active={active.bold} onClick={() => chain().toggleBold().run()}><b>B</b></ToolButton>
        <ToolButton label="کج" active={active.italic} onClick={() => chain().toggleItalic().run()}><i>I</i></ToolButton>
        <ToolButton label="زیرخط" active={active.underline} onClick={() => chain().toggleUnderline().run()}><u>U</u></ToolButton>
        <Divider />
        <ToolButton label="تیتر ۲" active={active.h2} onClick={() => chain().toggleHeading({ level: 2 }).run()}>H2</ToolButton>
        <ToolButton label="تیتر ۳" active={active.h3} onClick={() => chain().toggleHeading({ level: 3 }).run()}>H3</ToolButton>
        <ToolButton label="تیتر ۴" active={active.h4} onClick={() => chain().toggleHeading({ level: 4 }).run()}>H4</ToolButton>
        <Divider />
        <ToolButton label="فهرست نقطه‌ای" active={active.bulletList} onClick={() => chain().toggleBulletList().run()}>• فهرست</ToolButton>
        <ToolButton label="فهرست شماره‌دار" active={active.orderedList} onClick={() => chain().toggleOrderedList().run()}>۱. فهرست</ToolButton>
        <Divider />
        <ToolButton label="راست‌چین" active={active.right} onClick={() => chain().setTextAlign("right").run()}>راست</ToolButton>
        <ToolButton label="وسط‌چین" active={active.center} onClick={() => chain().setTextAlign("center").run()}>وسط</ToolButton>
        <ToolButton label="چپ‌چین" active={active.left} onClick={() => chain().setTextAlign("left").run()}>چپ</ToolButton>
        <ToolButton label="تراز دو طرف" active={active.justify} onClick={() => chain().setTextAlign("justify").run()}>تراز</ToolButton>
        <Divider />
        <ToolButton label="لینک" active={active.link || panel === "link"} onClick={() => togglePanel("link")}>لینک</ToolButton>
        <ToolButton label="تصویر" active={panel === "image"} onClick={() => togglePanel("image")}>تصویر</ToolButton>
        <ToolButton label="درج جدول" active={false} onClick={() => chain().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run()}>جدول</ToolButton>
        {active.table ? (
          <>
            <ToolButton label="افزودن سطر" active={false} onClick={() => chain().addRowAfter().run()}>+ سطر</ToolButton>
            <ToolButton label="افزودن ستون" active={false} onClick={() => chain().addColumnAfter().run()}>+ ستون</ToolButton>
            <ToolButton label="حذف جدول" active={false} onClick={() => chain().deleteTable().run()}>حذف جدول</ToolButton>
          </>
        ) : null}
        <Divider />
        <ToolButton label="واگرد" active={false} onClick={() => chain().undo().run()}>↶</ToolButton>
        <ToolButton label="از نو" active={false} onClick={() => chain().redo().run()}>↷</ToolButton>
      </div>
      {panel === "link" ? <LinkPanel editor={editor} onClose={() => setPanel(null)} /> : null}
      {panel === "image" ? <ImagePanel editor={editor} onClose={() => setPanel(null)} /> : null}
    </div>
  );
}

function LinkPanel({ editor, onClose }: { editor: Editor; onClose: () => void }) {
  const [url, setUrl] = useState(() => (editor.getAttributes("link").href as string | undefined) ?? "");
  const [error, setError] = useState<string>();
  const isLink = editor.isActive("link");

  function apply() {
    const href = normalizeEditorUrl(url);
    if (!href) {
      setError("نشانی باید با https:// یا http:// یا / شروع شود.");
      return;
    }
    if (editor.state.selection.empty && !isLink) {
      // Nothing selected: insert the address itself as the link text.
      editor.chain().focus().insertContent({ type: "text", text: href, marks: [{ type: "link", attrs: { href } }] }).run();
    } else {
      editor.chain().focus().extendMarkRange("link").setLink({ href }).run();
    }
    onClose();
  }

  function remove() {
    editor.chain().focus().extendMarkRange("link").unsetLink().run();
    onClose();
  }

  return (
    <UrlPanel label="نشانی لینک" value={url} onChange={setUrl} error={error} onApply={apply} onCancel={onClose}>
      {isLink ? <Button type="button" variant="ghost" className="min-h-9" onClick={remove}>حذف لینک</Button> : null}
    </UrlPanel>
  );
}

function ImagePanel({ editor, onClose }: { editor: Editor; onClose: () => void }) {
  const [url, setUrl] = useState("");
  const [alt, setAlt] = useState("");
  const [error, setError] = useState<string>();

  function apply() {
    const src = normalizeEditorUrl(url);
    if (!src) {
      setError("نشانی تصویر باید با https:// یا http:// یا / شروع شود.");
      return;
    }
    editor.chain().focus().setImage({ src, alt: alt.trim() || undefined }).run();
    onClose();
  }

  return (
    <UrlPanel label="نشانی تصویر" value={url} onChange={setUrl} error={error} onApply={apply} onCancel={onClose}>
      <Input className="min-h-9 max-w-48" aria-label="متن جایگزین تصویر" placeholder="متن جایگزین (alt)" value={alt} onChange={(event) => setAlt(event.target.value)} />
    </UrlPanel>
  );
}

/** Not a nested <form> (invalid inside the product form): Enter applies, Escape cancels. */
function UrlPanel({
  label,
  value,
  onChange,
  error,
  onApply,
  onCancel,
  children,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  onApply: () => void;
  onCancel: () => void;
  children?: ReactNode;
}) {
  return (
    <div className="space-y-1 border-t border-[var(--border)] p-2">
      <div className="flex flex-wrap items-center gap-2">
        <Input
          className="min-h-9 max-w-md flex-1"
          dir="ltr"
          aria-label={label}
          placeholder="https://"
          value={value}
          autoFocus
          onChange={(event) => onChange(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter") {
              event.preventDefault();
              onApply();
            } else if (event.key === "Escape") {
              onCancel();
            }
          }}
        />
        {children}
        <Button type="button" className="min-h-9" onClick={onApply}>اعمال</Button>
        <Button type="button" variant="ghost" className="min-h-9" onClick={onCancel}>انصراف</Button>
      </div>
      {error ? <p className="text-xs text-[var(--danger)]">{error}</p> : null}
    </div>
  );
}

function ToolButton({ label, active, onClick, children }: { label: string; active: boolean; onClick: () => void; children: ReactNode }) {
  return (
    <button
      type="button"
      title={label}
      aria-label={label}
      aria-pressed={active}
      className={`min-h-8 rounded-md px-2 text-xs font-bold transition ${active ? "bg-[var(--primary)] text-white" : "text-[var(--foreground)] hover:bg-black/5"}`}
      // Keeps the editor's selection while clicking the toolbar.
      onMouseDown={(event) => event.preventDefault()}
      onClick={onClick}
    >
      {children}
    </button>
  );
}

function Divider() {
  return <span className="mx-1 h-5 w-px bg-[var(--border)]" aria-hidden />;
}
