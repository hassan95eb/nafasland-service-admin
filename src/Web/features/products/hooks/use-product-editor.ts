"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import {
  createProduct,
  getCategories,
  getFilters,
  updateProduct,
  type CreateProductRequest,
  type UpdateProductRequest,
} from "@/features/products/api/product-editor";
import { productQueryKeys } from "@/features/products/api/list-products";

export function useProductTaxonomy() {
  const categories = useQuery({ queryKey: productQueryKeys.categories(), queryFn: getCategories });
  const filters = useQuery({ queryKey: productQueryKeys.filters(), queryFn: getFilters });
  return { categories, filters, isStale: Boolean(categories.data?.isStale || filters.data?.isStale) };
}

export function useCreateProduct() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ request, idempotencyKey }: { request: CreateProductRequest; idempotencyKey: string }) =>
      createProduct(request, idempotencyKey),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: productQueryKeys.all }),
  });
}

export function useUpdateProduct(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdateProductRequest) => updateProduct(id, request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: productQueryKeys.detail(id) });
      await queryClient.invalidateQueries({ queryKey: productQueryKeys.all });
    },
  });
}
