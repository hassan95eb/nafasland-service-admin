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
import { formatPersianNumber } from "@/shared/lib/formatters";
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

  const [fullscreen, setFullscreen] = useState(false);

  useEffect(() => {
    if (!fullscreen) return;
    const exit = (event: KeyboardEvent) => { if (event.key === "Escape") setFullscreen(false); };
    const overflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    window.addEventListener("keydown", exit);
    return () => {
      document.body.style.overflow = overflow;
      window.removeEventListener("keydown", exit);
    };
  }, [fullscreen]);

  return (
    <div className="space-y-2">
      <label className="text-sm font-bold" htmlFor={id}>{label}</label>
      {warnings.length > 0 ? (
        <Alert tone="warning">
          {warnings[0]} برای جلوگیری از حذف بی‌صدا، متن اصلی از نمای کد HTML قابل ویرایش است.
        </Alert>
      ) : null}
      <div
        className={fullscreen
          ? "fixed inset-0 z-50 flex flex-col bg-white"
          : "overflow-hidden rounded-lg border border-[var(--border)] bg-white shadow-sm"}
      >
        {editor ? (
          <Toolbar
            editor={editor}
            htmlMode={state.mode === "html"}
            onHtmlMode={(html) => onChange(switchRichTextMode(state, html ? "html" : "visual"))}
            fullscreen={fullscreen}
            onFullscreen={() => setFullscreen(!fullscreen)}
          />
        ) : null}
        {state.mode === "html" ? (
          <textarea
            id={id}
            className={`w-full resize-y bg-white p-3 font-mono text-sm outline-none ${fullscreen ? "flex-1" : "min-h-52"}`}
            dir="ltr"
            value={state.value}
            onChange={(event) => onChange(changeRichText(state, event.target.value))}
          />
        ) : (
          <div
            id={id}
            className={`cursor-text p-4 leading-8 [&_.tiptap]:min-h-40 [&_.tiptap]:outline-none [&_.tiptap_a]:text-[var(--primary)] [&_.tiptap_a]:underline [&_.tiptap_h2]:text-xl [&_.tiptap_h2]:font-black [&_.tiptap_h3]:text-lg [&_.tiptap_h3]:font-bold [&_.tiptap_h4]:font-bold [&_.tiptap_img]:max-h-64 [&_.tiptap_ol]:list-decimal [&_.tiptap_ol]:pr-6 [&_.tiptap_table]:w-full [&_.tiptap_td]:border [&_.tiptap_td]:p-2 [&_.tiptap_th]:border [&_.tiptap_th]:bg-neutral-50 [&_.tiptap_th]:p-2 [&_.tiptap_ul]:list-disc [&_.tiptap_ul]:pr-6 ${fullscreen ? "flex-1 overflow-y-auto" : ""}`}
            onClick={() => editor?.commands.focus()}
          >
            <EditorContent editor={editor} />
          </div>
        )}
        {editor ? <CharacterCount editor={editor} htmlLength={state.mode === "html" ? state.value.length : undefined} /> : null}
      </div>
    </div>
  );
}

function CharacterCount({ editor, htmlLength }: { editor: Editor; htmlLength?: number }) {
  const textLength = useEditorState({ editor, selector: ({ editor: current }) => current.state.doc.textContent.length });
  return (
    <div className="flex justify-end border-t border-[var(--border)] px-3 py-1 text-xs text-[var(--muted)]">
      {htmlLength == null
        ? `${formatPersianNumber(textLength)} نویسه`
        : `${formatPersianNumber(htmlLength)} نویسهٔ HTML`}
    </div>
  );
}

type Panel = "link" | "image" | null;

const BLOCK_STYLES = [
  { key: "paragraph", label: "متن عادی", className: "text-sm" },
  { key: "h2", label: "تیتر ۲", className: "text-lg font-black" },
  { key: "h3", label: "تیتر ۳", className: "text-base font-bold" },
  { key: "h4", label: "تیتر ۴", className: "text-sm font-bold" },
] as const;

const ALIGNMENTS = [
  { key: "right", label: "راست‌چین" },
  { key: "center", label: "وسط‌چین" },
  { key: "left", label: "چپ‌چین" },
  { key: "justify", label: "تراز دو طرف" },
] as const;

function Toolbar({ editor, htmlMode, onHtmlMode, fullscreen, onFullscreen }: {
  editor: Editor;
  htmlMode: boolean;
  onHtmlMode: (html: boolean) => void;
  fullscreen: boolean;
  onFullscreen: () => void;
}) {
  const [panel, setPanel] = useState<Panel>(null);
  const active = useEditorState({
    editor,
    selector: ({ editor: current }) => ({
      bold: current.isActive("bold"),
      italic: current.isActive("italic"),
      underline: current.isActive("underline"),
      block: current.isActive("heading", { level: 2 }) ? "h2"
        : current.isActive("heading", { level: 3 }) ? "h3"
          : current.isActive("heading", { level: 4 }) ? "h4" : "paragraph",
      align: (["center", "left", "justify"] as const).find((value) => current.isActive({ textAlign: value })) ?? "right",
      bulletList: current.isActive("bulletList"),
      orderedList: current.isActive("orderedList"),
      canIndent: current.can().sinkListItem("listItem"),
      canOutdent: current.can().liftListItem("listItem"),
      link: current.isActive("link"),
      table: current.isActive("table"),
      canUndo: current.can().undo(),
      canRedo: current.can().redo(),
    }),
  });
  const chain = () => editor.chain().focus();
  const togglePanel = (next: Exclude<Panel, null>) => setPanel(panel === next ? null : next);
  const off = htmlMode;

  function setBlock(key: (typeof BLOCK_STYLES)[number]["key"]) {
    if (key === "paragraph") chain().setParagraph().run();
    else chain().setHeading({ level: Number(key.slice(1)) as 2 | 3 | 4 }).run();
  }

  return (
    <div className="border-b border-[var(--border)] bg-neutral-50">
      <div className="flex flex-wrap items-center gap-y-1 p-1.5" role="toolbar" aria-label="ابزار قالب‌بندی">
        <Group>
          <Dropdown label="سبک پاراگراف" disabled={off} trigger={<span className="text-base leading-none">¶</span>}>
            {(close) => BLOCK_STYLES.map((item) => (
              <MenuItem key={item.key} active={active.block === item.key} onSelect={() => { setBlock(item.key); close(); }}>
                <span className={item.className}>{item.label}</span>
              </MenuItem>
            ))}
          </Dropdown>
        </Group>
        <Group>
          <ToolButton label="پررنگ" active={active.bold} disabled={off} onClick={() => chain().toggleBold().run()}><b className="font-serif text-[15px]">B</b></ToolButton>
          <ToolButton label="کج" active={active.italic} disabled={off} onClick={() => chain().toggleItalic().run()}><i className="font-serif text-[15px] font-bold">I</i></ToolButton>
          <ToolButton label="زیرخط" active={active.underline} disabled={off} onClick={() => chain().toggleUnderline().run()}><u className="font-serif text-[15px] font-bold">U</u></ToolButton>
        </Group>
        <Group>
          <Dropdown label="چینش" disabled={off} trigger={<Icon name={`align-${active.align}`} />}>
            {(close) => (
              <div className="flex gap-1 p-1">
                {ALIGNMENTS.map((item) => (
                  <ToolButton key={item.key} label={item.label} active={active.align === item.key} onClick={() => { chain().setTextAlign(item.key).run(); close(); }}>
                    <Icon name={`align-${item.key}`} />
                  </ToolButton>
                ))}
              </div>
            )}
          </Dropdown>
          <ToolButton label="فهرست نقطه‌ای" active={active.bulletList} disabled={off} onClick={() => chain().toggleBulletList().run()}><Icon name="list-bullet" /></ToolButton>
          <ToolButton label="فهرست شماره‌دار" active={active.orderedList} disabled={off} onClick={() => chain().toggleOrderedList().run()}><Icon name="list-ordered" /></ToolButton>
          <ToolButton label="افزایش تورفتگی" active={false} disabled={off || !active.canIndent} onClick={() => chain().sinkListItem("listItem").run()}><Icon name="indent" /></ToolButton>
          <ToolButton label="کاهش تورفتگی" active={false} disabled={off || !active.canOutdent} onClick={() => chain().liftListItem("listItem").run()}><Icon name="outdent" /></ToolButton>
        </Group>
        <Group>
          <ToolButton label="لینک" active={active.link || panel === "link"} disabled={off} onClick={() => togglePanel("link")}><Icon name="link" /></ToolButton>
          <ToolButton label="تصویر" active={panel === "image"} disabled={off} onClick={() => togglePanel("image")}><Icon name="image" /></ToolButton>
          <ToolButton label="درج جدول" active={active.table} disabled={off} onClick={() => chain().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run()}><Icon name="table" /></ToolButton>
        </Group>
        {active.table && !off ? (
          <Group>
            <ToolButton label="افزودن سطر" active={false} onClick={() => chain().addRowAfter().run()}><span className="text-xs">+ سطر</span></ToolButton>
            <ToolButton label="افزودن ستون" active={false} onClick={() => chain().addColumnAfter().run()}><span className="text-xs">+ ستون</span></ToolButton>
            <ToolButton label="حذف سطر" active={false} onClick={() => chain().deleteRow().run()}><span className="text-xs">− سطر</span></ToolButton>
            <ToolButton label="حذف ستون" active={false} onClick={() => chain().deleteColumn().run()}><span className="text-xs">− ستون</span></ToolButton>
            <ToolButton label="حذف جدول" active={false} onClick={() => chain().deleteTable().run()}><span className="text-xs text-[var(--danger)]">حذف جدول</span></ToolButton>
          </Group>
        ) : null}
        <Group>
          <ToolButton label="پاک کردن قالب‌بندی" active={false} disabled={off} onClick={() => chain().unsetAllMarks().clearNodes().run()}><Icon name="eraser" /></ToolButton>
          <ToolButton label={fullscreen ? "خروج از تمام‌صفحه" : "تمام‌صفحه"} active={fullscreen} onClick={onFullscreen}><Icon name={fullscreen ? "minimize" : "maximize"} /></ToolButton>
          <ToolButton label="نمای کد HTML" active={htmlMode} onClick={() => { setPanel(null); onHtmlMode(!htmlMode); }}><Icon name="code" /></ToolButton>
        </Group>
        <Group last>
          <ToolButton label="واگرد" active={false} disabled={off || !active.canUndo} onClick={() => chain().undo().run()}><Icon name="undo" /></ToolButton>
          <ToolButton label="از نو" active={false} disabled={off || !active.canRedo} onClick={() => chain().redo().run()}><Icon name="redo" /></ToolButton>
        </Group>
      </div>
      {panel === "link" && !off ? <LinkPanel editor={editor} onClose={() => setPanel(null)} /> : null}
      {panel === "image" && !off ? <ImagePanel editor={editor} onClose={() => setPanel(null)} /> : null}
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

function Group({ children, last = false }: { children: ReactNode; last?: boolean }) {
  return (
    <div className={`flex items-center gap-0.5 px-1 ${last ? "" : "border-e border-[var(--border)]"}`}>
      {children}
    </div>
  );
}

function ToolButton({ label, active, disabled = false, onClick, children }: {
  label: string;
  active: boolean;
  disabled?: boolean;
  onClick: () => void;
  children: ReactNode;
}) {
  return (
    <button
      type="button"
      title={label}
      aria-label={label}
      aria-pressed={active && !disabled}
      disabled={disabled}
      className={`grid h-8 min-w-8 place-items-center rounded-md px-1.5 transition disabled:cursor-not-allowed disabled:opacity-35 ${active && !disabled ? "bg-[var(--primary)] text-white" : "text-[var(--foreground)] hover:bg-black/5"}`}
      // Keeps the editor's selection while clicking the toolbar.
      onMouseDown={(event) => event.preventDefault()}
      onClick={onClick}
    >
      {children}
    </button>
  );
}

function Dropdown({ label, trigger, disabled = false, children }: {
  label: string;
  trigger: ReactNode;
  disabled?: boolean;
  children: (close: () => void) => ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const root = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const outside = (event: MouseEvent) => { if (!root.current?.contains(event.target as Node)) setOpen(false); };
    const escape = (event: KeyboardEvent) => { if (event.key === "Escape") setOpen(false); };
    document.addEventListener("mousedown", outside);
    document.addEventListener("keydown", escape);
    return () => {
      document.removeEventListener("mousedown", outside);
      document.removeEventListener("keydown", escape);
    };
  }, [open]);

  return (
    <div ref={root} className="relative">
      <button
        type="button"
        title={label}
        aria-label={label}
        aria-haspopup="menu"
        aria-expanded={open}
        disabled={disabled}
        className="flex h-8 items-center gap-1 rounded-md px-1.5 text-[var(--foreground)] transition hover:bg-black/5 disabled:cursor-not-allowed disabled:opacity-35"
        onMouseDown={(event) => event.preventDefault()}
        onClick={() => setOpen(!open)}
      >
        {trigger}
        <Icon name="caret" className="size-2.5" />
      </button>
      {open ? (
        <div role="menu" className="absolute start-0 top-full z-20 mt-1 min-w-36 rounded-lg border border-[var(--border)] bg-white py-1 shadow-lg">
          {children(() => setOpen(false))}
        </div>
      ) : null}
    </div>
  );
}

function MenuItem({ active, onSelect, children }: { active: boolean; onSelect: () => void; children: ReactNode }) {
  return (
    <button
      type="button"
      role="menuitemradio"
      aria-checked={active}
      className={`block w-full px-3 py-1.5 text-right hover:bg-black/5 ${active ? "text-[var(--primary)]" : ""}`}
      onMouseDown={(event) => event.preventDefault()}
      onClick={onSelect}
    >
      {children}
    </button>
  );
}

const ICON_PATHS: Record<string, string> = {
  "align-right": "M4 6h16M10 10h10M4 14h16M10 18h10",
  "align-center": "M4 6h16M7 10h10M4 14h16M7 18h10",
  "align-left": "M4 6h16M4 10h10M4 14h16M4 18h10",
  "align-justify": "M4 6h16M4 10h16M4 14h16M4 18h16",
  "list-bullet": "M10 6h10M10 12h10M10 18h10M5 6h.01M5 12h.01M5 18h.01",
  "list-ordered": "M10 6h10M10 12h10M10 18h10M4 5l1.5-1v5M4 14.5a1.5 1.5 0 0 1 3 .2c0 .8-3 2.3-3 3.3h3",
  indent: "M4 5h16M4 19h16M11 10h9M11 14h9M8 12l-4-3v6z",
  outdent: "M4 5h16M4 19h16M4 10h9M4 14h9M16 12l4-3v6z",
  link: "M10 14a4 4 0 0 0 5.7 0l3-3a4 4 0 0 0-5.7-5.7l-1 1M14 10a4 4 0 0 0-5.7 0l-3 3a4 4 0 0 0 5.7 5.7l1-1",
  image: "M4 5h16v14H4zM4 16l5-5 4 4 2-2 5 5M15 9.5h.01",
  table: "M4 5h16v14H4zM4 10h16M4 15h16M10 5v14",
  eraser: "M9 20h11M5 16l9-9 5 5-6.5 6.5H9.5zM10 11l5 5",
  maximize: "M14 4h6v6M10 20H4v-6M20 4l-7 7M4 20l7-7",
  minimize: "M4 14h6v6M20 10h-6V4M10 14l-7 7M14 10l7-7",
  code: "M9 8l-5 4 5 4M15 8l5 4-5 4M13.5 5l-3 14",
  undo: "M9 14L4 9l5-5M4 9h10a6 6 0 0 1 0 12h-3",
  redo: "M15 14l5-5-5-5M20 9H10a6 6 0 0 0 0 12h3",
  caret: "M6 9l6 6 6-6",
};

function Icon({ name, className = "size-4" }: { name: string; className?: string }) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className={className} fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d={ICON_PATHS[name]} />
    </svg>
  );
}
