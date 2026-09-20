"use client";

import { useQuery } from "@tanstack/react-query";

import { listProducts, productQueryKeys, type ProductListParams } from "@/features/products/api/list-products";

export function useProducts(params: ProductListParams) {
  return useQuery({
    queryKey: productQueryKeys.list(params),
    queryFn: () => listProducts(params),
    placeholderData: (previous) => previous,
  });
}
