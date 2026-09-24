using System.Text.Json;
using System.Text.Json.Nodes;

namespace NafasLand.Admin.Modules.Auditing.Features.Queries;

/// <summary>
/// No command in steps 0–8 puts a secret into Before/After (checked when this
/// was written: ResetPassword and CreateUser snapshot only flags and the
/// username). This is the guard for the next one that might — any key, at any
/// depth, that looks like a password, hash, secret or token is masked before it
/// leaves the server, whatever the record's Action.
/// </summary>
internal static class SensitiveAuditFields
{
    public const string Mask = "***";

    private static readonly string[] SensitiveKeyFragments = ["password", "hash", "secret", "token"];

    public static bool IsSensitive(string key) =>
        SensitiveKeyFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    public static JsonElement? ParseAndMask(string? json)
    {
        if (json is null)
        {
            return null;
        }

        var node = JsonNode.Parse(json);
        MaskInPlace(node);
        return JsonSerializer.SerializeToElement(node);
    }

    private static void MaskInPlace(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var key in jsonObject.Select(property => property.Key).ToList())
                {
                    if (IsSensitive(key))
                    {
                        jsonObject[key] = Mask;
                    }
                    else
                    {
                        MaskInPlace(jsonObject[key]);
                    }
                }

                break;
            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                {
                    MaskInPlace(item);
                }

                break;
        }
    }
}
