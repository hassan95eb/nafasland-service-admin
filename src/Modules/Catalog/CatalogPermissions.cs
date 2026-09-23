namespace NafasLand.Admin.Modules.Catalog;

internal static class CatalogPermissions
{
    public const string ProductsRead = "catalog.products.read";
    public const string ProductsWrite = "catalog.products.write";

    // ADR-010/ADR-030/ADR-031/ADR-032 write their permission keys without a
    // module prefix ("product.delete.request", "product.publish", ...) — the
    // same mismatch step 6 already found and corrected for
    // "catalog.products.write" vs. the ADR's original "product.price.update"
    // (see prompts/step-06-price-inventory-write.md). The actual convention in
    // this codebase is a lowercase module prefix, as above; these follow it
    // instead of the ADRs' raw form. Reported in the PR description for
    // DECISIONS.md to be corrected.
    public const string ProductsDeleteRequest = "catalog.products.delete.request";
    public const string ProductsDelete = "catalog.products.delete";
    public const string ProductsPublishRequest = "catalog.products.publish.request";
    public const string ProductsPublish = "catalog.products.publish";
    public const string ProductsStatusRequest = "catalog.products.status.request";
    public const string ProductsStatus = "catalog.products.status";
    public const string VariantsDeleteRequest = "catalog.variants.delete.request";
    public const string VariantsDelete = "catalog.variants.delete";
}
