/**
 * Mirrors the backend guard in DeleteVariantApprovalExecutor (ADR-032): a
 * product's last variant cannot be requested for deletion, since removing it
 * would leave the product priceless and unpurchasable.
 */
export function isLastVariant(variantCount: number) {
  return variantCount <= 1;
}
