"use client";

import { useMemo, useState } from "react";

import type { useProductTaxonomy } from "@/features/products/hooks/use-product-editor";
import { isSelectableCategory } from "@/features/products/schemas/product-editor-schema";
import { Input } from "@/shared/ui/input";

type Categories = NonNullable<ReturnType<typeof useProductTaxonomy>["categories"]["data"]>["items"];
type CategoryNode = Categories[number];
type Filters = NonNullable<ReturnType<typeof useProductTaxonomy>["filters"]["data"]>["items"];
type FilterGroup = Filters[number];

function toggle(values: Set<string>, id: string) {
  const next = new Set(values);
  if (next.has(id)) next.delete(id); else next.add(id);
  return next;
}

function includesTerm(title: string | null, term: string) {
  return (title ?? "").toLowerCase().includes(term);
}

function categoryMatches(item: CategoryNode, term: string): boolean {
  if (!term) return true;
  if (includesTerm(item.title, term)) return true;
  return item.children.some((child) => categoryMatches(child, term));
}

function hasSelectedDescendant(item: CategoryNode, selected: Set<string>): boolean {
  return item.children.some((child) => selected.has(child.id) || hasSelectedDescendant(child, selected));
}

export function TaxonomySelectors({ categories, filters, categoryIds, filterIds, onCategoryIds, onFilterIds }: {
  categories: Categories;
  filters: Filters;
  categoryIds: Set<string>;
  filterIds: Set<string>;
  onCategoryIds: (value: Set<string>) => void;
  onFilterIds: (value: Set<string>) => void;
}) {
  const [categorySearch, setCategorySearch] = useState("");
  const [filterSearch, setFilterSearch] = useState("");
  const categoryTerm = categorySearch.trim().toLowerCase();
  const filterTerm = filterSearch.trim().toLowerCase();

  const visibleCategories = useMemo(
    () => categories.filter((item) => categoryMatches(item, categoryTerm)),
    [categories, categoryTerm],
  );
  const visibleFilterGroups = useMemo(() => {
    if (!filterTerm) return filters;
    return filters
      .map((group) => ({ ...group, values: group.values.filter((value) => includesTerm(value.title, filterTerm)) }))
      .filter((group) => includesTerm(group.title, filterTerm) || group.values.length > 0);
  }, [filters, filterTerm]);

  return (
    <section className="grid gap-5 rounded-xl border border-[var(--border)] bg-white p-5 md:grid-cols-2">
      <div className="space-y-3">
        <h2 className="font-black">دسته‌بندی‌ها</h2>
        <Input
          type="search"
          placeholder="جست‌وجوی دسته‌بندی…"
          aria-label="جست‌وجوی دسته‌بندی"
          value={categorySearch}
          onChange={(event) => setCategorySearch(event.target.value)}
        />
        <div className="max-h-80 space-y-1 overflow-y-auto rounded-lg border border-[var(--border)] p-3">
          {visibleCategories.length === 0 ? (
            <p className="text-sm text-[var(--muted)]">دسته‌بندی‌ای پیدا نشد.</p>
          ) : (
            visibleCategories.map((item) => (
              <CategoryItem key={item.id} item={item} selected={categoryIds} onChange={onCategoryIds} depth={0} term={categoryTerm} />
            ))
          )}
        </div>
      </div>

      <div className="space-y-3">
        <h2 className="font-black">فیلترها</h2>
        <Input
          type="search"
          placeholder="جست‌وجوی فیلتر…"
          aria-label="جست‌وجوی فیلتر"
          value={filterSearch}
          onChange={(event) => setFilterSearch(event.target.value)}
        />
        <div className="max-h-80 space-y-2 overflow-y-auto rounded-lg border border-[var(--border)] p-3">
          {visibleFilterGroups.length === 0 ? (
            <p className="text-sm text-[var(--muted)]">فیلتری پیدا نشد.</p>
          ) : (
            visibleFilterGroups.map((group) => (
              <FilterGroupItem key={group.id} group={group} selected={filterIds} onChange={onFilterIds} forceOpen={Boolean(filterTerm)} />
            ))
          )}
        </div>
      </div>
    </section>
  );
}

function CategoryItem({ item, selected, onChange, depth, term }: {
  item: CategoryNode;
  selected: Set<string>;
  onChange: (value: Set<string>) => void;
  depth: number;
  term: string;
}) {
  const visibleChildren = item.children.filter((child) => categoryMatches(child, term));
  const label = (
    <label className="flex items-center gap-2 text-sm">
      <input
        type="checkbox"
        disabled={!isSelectableCategory(item.type)}
        checked={selected.has(item.id)}
        onClick={(event) => event.stopPropagation()}
        onChange={() => onChange(toggle(selected, item.id))}
      />
      {item.title}
      {!isSelectableCategory(item.type) ? <span className="text-[var(--muted)]">(والد)</span> : null}
    </label>
  );

  if (visibleChildren.length === 0) {
    return <div style={{ paddingInlineStart: `${depth * 16}px` }}>{label}</div>;
  }

  return (
    <details style={{ paddingInlineStart: `${depth * 16}px` }} open={Boolean(term) || hasSelectedDescendant(item, selected)}>
      <summary className="cursor-pointer">{label}</summary>
      <div className="mt-1 space-y-1">
        {visibleChildren.map((child) => (
          <CategoryItem key={child.id} item={child} selected={selected} onChange={onChange} depth={depth + 1} term={term} />
        ))}
      </div>
    </details>
  );
}

function FilterGroupItem({ group, selected, onChange, forceOpen }: {
  group: FilterGroup;
  selected: Set<string>;
  onChange: (value: Set<string>) => void;
  forceOpen: boolean;
}) {
  const selectedCount = group.values.filter((value) => selected.has(value.id)).length;

  return (
    <details className="rounded-lg border border-[var(--border)] p-3" open={forceOpen || selectedCount > 0}>
      <summary className="cursor-pointer text-sm font-bold">
        {group.title}
        {selectedCount > 0 ? <span className="text-[var(--primary)]"> ({selectedCount})</span> : null}
      </summary>
      <fieldset className="mt-2 space-y-2">
        {group.values.map((value) => (
          <label className="flex gap-2 text-sm" key={value.id}>
            <input type="checkbox" checked={selected.has(value.id)} onChange={() => onChange(toggle(selected, value.id))} />
            {value.title}
          </label>
        ))}
      </fieldset>
    </details>
  );
}
