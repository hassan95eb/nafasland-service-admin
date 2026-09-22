"use client";

import { useMemo } from "react";

import { sanitizeProductHtml } from "@/shared/lib/sanitize-html";

export function SafeProductHtml({ html }: { html: string }) {
  const clean = useMemo(() => sanitizeProductHtml(html), [html]);
  return <div className="prose max-w-none" dangerouslySetInnerHTML={{ __html: clean }} />;
}
