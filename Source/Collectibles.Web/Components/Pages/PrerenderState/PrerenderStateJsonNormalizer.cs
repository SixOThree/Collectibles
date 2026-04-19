using System.Text.Json;

namespace Collectibles.Web.Components.Pages.PrerenderState;

internal static class PrerenderStateJsonNormalizer
{
    public static Dictionary<string, object?> NormalizeDictionary(Dictionary<string, object?> values)
    {
        foreach (var key in values.Keys.ToList())
        {
            values[key] = NormalizeValue(values[key]);
        }

        return values;
    }

    public static List<Dictionary<string, object?>>? NormalizeDictionaryList(List<Dictionary<string, object?>>? values)
    {
        if (values == null)
        {
            return null;
        }

        for (var index = 0; index < values.Count; index++)
        {
            values[index] = NormalizeDictionary(values[index]);
        }

        return values;
    }

    private static object? NormalizeValue(object? value)
    {
        return value switch
        {
            JsonElement element => NormalizeJsonElement(element),
            List<object?> list => list.Select(NormalizeValue).ToList(),
            Dictionary<string, object?> dictionary => NormalizeDictionary(dictionary),
            _ => value
        };
    }

    private static object? NormalizeJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longValue)
                ? longValue
                : element.TryGetDecimal(out var decimalValue)
                    ? decimalValue
                    : element.ToString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.Array => element.GetRawText(),
            JsonValueKind.Object => element.GetRawText(),
            _ => element.ToString()
        };
    }
}
