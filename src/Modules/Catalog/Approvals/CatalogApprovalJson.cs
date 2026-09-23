using System.Text.Json;

namespace NafasLand.Admin.Modules.Catalog.Approvals;

internal static class CatalogApprovalJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
