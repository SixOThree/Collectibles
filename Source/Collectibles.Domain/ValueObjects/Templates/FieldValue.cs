using System.Text.Json;

namespace Collectibles.Domain.ValueObjects.Templates;

/// <summary>
/// Represents a field value with its associated field name.
/// </summary>
public class FieldValue
{
    /// <summary>
    /// Gets or sets the field name this value belongs to.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw value as stored.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets the value as a string.
    /// </summary>
    /// <returns></returns>
    public string? AsString()
    {
        return Value?.ToString();
    }

    /// <summary>
    /// Gets the value as a boolean.
    /// </summary>
    /// <returns></returns>
    public bool? AsBoolean()
    {
        if (Value == null)
        {
            return null;
        }

        if (Value is bool b)
        {
            return b;
        }

        if (bool.TryParse(Value.ToString(), out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Gets the value as a number.
    /// </summary>
    /// <returns></returns>
    public decimal? AsDecimal()
    {
        if (Value == null)
        {
            return null;
        }

        if (Value is decimal d)
        {
            return d;
        }

        if (decimal.TryParse(Value.ToString(), out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Gets the value as a date.
    /// </summary>
    /// <returns></returns>
    public DateTime? AsDateTime()
    {
        if (Value == null)
        {
            return null;
        }

        if (Value is DateTime dt)
        {
            return dt;
        }

        if (DateTime.TryParse(Value.ToString(), out var result))
        {
            return result;
        }

        return null;
    }
}

/// <summary>
/// Represents a collection of field values for a collectible item.
/// </summary>
public class FieldValueCollection
{
    private readonly Dictionary<string, FieldValue> _values = new();

    /// <summary>
    /// Gets or sets a field value by field name.
    /// </summary>
    public FieldValue? this[string fieldName]
    {
        get => _values.TryGetValue(fieldName, out var value) ? value : null;
        set
        {
            if (value == null)
            {
                _values.Remove(fieldName);
            }
            else
            {
                _values[fieldName] = value;
            }
        }
    }

    /// <summary>
    /// Gets all field values.
    /// </summary>
    public IReadOnlyDictionary<string, FieldValue> Values => _values;

    /// <summary>
    /// Sets a field value.
    /// </summary>
    public void SetValue(string fieldName, object? value)
    {
        this[fieldName] = new FieldValue { FieldName = fieldName, Value = value };
    }

    /// <summary>
    /// Serializes the field values to JSON.
    /// </summary>
    /// <returns></returns>
    public string ToJson()
    {
        var data = _values.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Value);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        return JsonSerializer.Serialize(data, options);
    }

    /// <summary>
    /// Deserializes field values from JSON.
    /// </summary>
    /// <returns></returns>
    public static FieldValueCollection FromJson(string? json)
    {
        var collection = new FieldValueCollection();

        if (string.IsNullOrWhiteSpace(json))
        {
            return collection;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
        if (data != null)
        {
            foreach (var kvp in data)
            {
                object? value = kvp.Value.ValueKind switch
                {
                    JsonValueKind.String => kvp.Value.GetString(),
                    JsonValueKind.Number => kvp.Value.TryGetDecimal(out var d) ? d : kvp.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => kvp.Value.GetRawText(),
                };

                collection.SetValue(kvp.Key, value);
            }
        }

        return collection;
    }
}
