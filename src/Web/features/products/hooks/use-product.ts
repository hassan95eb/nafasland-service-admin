"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { getProduct } from "@/features/products/api/get-product";
import { productQueryKeys } from "@/features/products/api/list-products";
import { updateVariant } from "@/features/products/api/update-variant";

export function useProduct(id: string) {
  return useQuery({ queryKey: productQueryKeys.detail(id), queryFn: () => getProduct(id) });
}

export function useUpdateVariant(productId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: updateVariant,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: productQueryKeys.detail(productId) }),
  });
}
