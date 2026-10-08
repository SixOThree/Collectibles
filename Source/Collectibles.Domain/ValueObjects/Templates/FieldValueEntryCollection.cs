using System.Text.Json;

namespace Collectibles.Domain.ValueObjects.Templates;

/// <summary>
/// Represents a single entry (row) in a multi-entry collectible item.
/// Each entry contains a set of field values matching the template's field definitions.
/// </summary>
public class FieldValueEntry
{
    /// <summary>
    /// Gets or sets the unique identifier for this entry (client-side tracking).
    /// </summary>
    public Guid EntryId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the display order of this entry.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the field values for this entry.
    /// </summary>
    public Dictionary<string, object?> Values { get; set; } = new();
}

/// <summary>
/// Represents a collection of entries for a multi-entry collectible item.
/// Stored as a JSON array in CollectibleItem.ContentValue.
/// </summary>
public class FieldValueEntryCollection
{
    private readonly List<FieldValueEntry> _entries = new();

    /// <summary>
    /// Gets all entries in the collection.
    /// </summary>
    public IReadOnlyList<FieldValueEntry> Entries => _entries.AsReadOnly();

    /// <summary>
    /// Gets the number of entries.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Adds an entry to the collection.
    /// </summary>
    public void AddEntry(FieldValueEntry entry)
    {
        _entries.Add(entry);
    }

    /// <summary>
    /// Removes an entry by its entry ID.
    /// </summary>
    /// <returns></returns>
    public bool RemoveEntry(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.EntryId == entryId);
        if (entry != null)
        {
            return _entries.Remove(entry);
        }

        return false;
    }

    /// <summary>
    /// Serializes the entry collection to a JSON array.
    /// </summary>
    /// <returns></returns>
    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        return JsonSerializer.Serialize(_entries, options);
    }

    /// <summary>
    /// Deserializes an entry collection from JSON.
    /// Handles: null/empty (returns empty), JSON array (multi-entry), JSON object (legacy single-entry wrapped as one entry).
    /// </summary>
    /// <returns></returns>
    public static FieldValueEntryCollection FromJson(string? json)
    {
        var collection = new FieldValueEntryCollection();

        if (string.IsNullOrWhiteSpace(json))
        {
            return collection;
        }

        var trimmed = json.TrimStart();

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        if (trimmed.StartsWith('['))
        {
            // Multi-entry JSON array
            var entries = JsonSerializer.Deserialize<List<JsonElement>>(json, options);
            if (entries != null)
            {
                foreach (var element in entries)
                {
                    var entry = DeserializeEntry(element, options);
                    collection.AddEntry(entry);
                }
            }
        }
        else if (trimmed.StartsWith('{'))
        {
            // Legacy single-entry JSON object — wrap as one entry
            var values = DeserializeValues(json, options);
            collection.AddEntry(new FieldValueEntry
            {
                SortOrder = 0,
                Values = values,
            });
        }

        return collection;
    }

    /// <summary>
    /// Creates an entry collection from a list of dictionaries (used by commands/DTOs).
    /// </summary>
    /// <returns></returns>
    public static FieldValueEntryCollection FromDictionaryList(List<Dictionary<string, object?>> entries)
    {
        var collection = new FieldValueEntryCollection();
        for (var i = 0; i < entries.Count; i++)
        {
            collection.AddEntry(new FieldValueEntry
            {
                SortOrder = i,
                Values = entries[i],
            });
        }

        return collection;
    }

    /// <summary>
    /// Converts the entry collection to a list of dictionaries (used by DTOs).
    /// </summary>
    /// <returns></returns>
    public List<Dictionary<string, object?>> ToDictionaryList()
    {
        return _entries
            .OrderBy(e => e.SortOrder)
            .Select(e => new Dictionary<string, object?>(e.Values))
            .ToList();
    }

    private static FieldValueEntry DeserializeEntry(JsonElement element, JsonSerializerOptions options)
    {
        var entry = new FieldValueEntry();

        if (element.TryGetProperty("entryId", out var entryIdProp) &&
            Guid.TryParse(entryIdProp.GetString(), out var entryId))
        {
            entry.EntryId = entryId;
        }

        if (element.TryGetProperty("sortOrder", out var sortOrderProp))
        {
            entry.SortOrder = sortOrderProp.GetInt32();
        }

        if (element.TryGetProperty("values", out var valuesProp))
        {
            entry.Values = DeserializeValuesDictionary(valuesProp);
        }

        return entry;
    }

    private static Dictionary<string, object?> DeserializeValues(string json, JsonSerializerOptions options)
    {
        var result = new Dictionary<string, object?>();
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
        if (data != null)
        {
            foreach (var kvp in data)
            {
                result[kvp.Key] = ConvertJsonElement(kvp.Value);
            }
        }

        return result;
    }

    private static Dictionary<string, object?> DeserializeValuesDictionary(JsonElement element)
    {
        var result = new Dictionary<string, object?>();
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                result[prop.Name] = ConvertJsonElement(prop.Value);
            }
        }

        return result;
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetDecimal(out var d) ? d : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText(),
        };
    }
}
