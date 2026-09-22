import DOMPurify from "dompurify";

export const productHtmlTags = [
  "p", "br", "strong", "b", "em", "i", "u", "ul", "ol", "li", "a", "img",
  "h2", "h3", "h4", "table", "thead", "tbody", "tr", "td", "th", "span", "div",
] as const;

const allowedAttributes = new Set(["style", "href", "src", "alt", "title"]);
const allowedStyles = new Set(["text-align", "font-weight"]);
const tiptapSupportedTags = new Set([
  "p", "br", "strong", "b", "em", "i", "u", "ul", "ol", "li", "a", "img",
  "h2", "h3", "h4", "table", "thead", "tbody", "tr", "td", "th",
]);

export function sanitizeAllowedStyle(style: string) {
  return style
    .split(";")
    .map((declaration) => declaration.trim())
    .filter(Boolean)
    .flatMap((declaration) => {
      const separator = declaration.indexOf(":");
      if (separator < 1) return [];
      const property = declaration.slice(0, separator).trim().toLowerCase();
      const value = declaration.slice(separator + 1).trim();
      return allowedStyles.has(property) && value ? [`${property}: ${value}`] : [];
    })
    .join("; ");
}

export function sanitizeProductHtml(html: string) {
  const hook = (_node: Element, data: { attrName: string; attrValue: string; keepAttr: boolean }) => {
    if (data.attrName.toLowerCase() === "style") {
      data.attrValue = sanitizeAllowedStyle(data.attrValue);
      data.keepAttr = Boolean(data.attrValue);
    }
  };
  DOMPurify.addHook("uponSanitizeAttribute", hook);
  try {
    return DOMPurify.sanitize(html, {
      ALLOWED_TAGS: [...productHtmlTags],
      ALLOWED_ATTR: [...allowedAttributes],
      ALLOW_DATA_ATTR: false,
      ALLOW_UNKNOWN_PROTOCOLS: false,
    });
  } finally {
    DOMPurify.removeHook("uponSanitizeAttribute");
  }
}

export function findUnsupportedHtml(html: string) {
  if (typeof DOMParser === "undefined") return [];
  const document = new DOMParser().parseFromString(html, "text/html");
  const warnings = new Set<string>();
  for (const element of document.body.querySelectorAll("*")) {
    const tag = element.tagName.toLowerCase();
    if (!tiptapSupportedTags.has(tag)) {
      warnings.add(`تگ <${tag}> در نمای دیداری پشتیبانی نمی‌شود.`);
    }
    for (const attribute of element.getAttributeNames()) {
      if (!allowedAttributes.has(attribute.toLowerCase())) {
        warnings.add(`ویژگی ${attribute} در نمای دیداری حذف می‌شود.`);
      }
    }
    const style = element.getAttribute("style");
    if (style && sanitizeAllowedStyle(style) !== style.trim().replace(/;$/, "")) {
      warnings.add("بخشی از style در allowlist گام ۷ نیست.");
    }
  }
  return [...warnings];
}
