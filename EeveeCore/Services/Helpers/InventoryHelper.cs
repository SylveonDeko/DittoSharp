using System.Text.Json;
using Serilog;

namespace EeveeCore.Services.Helpers;

/// <summary>
/// Helper service for safely handling inventory JSON deserialization across the application.
/// </summary>
public static class InventoryHelper
{
    /// <summary>
    /// Safely deserializes inventory JSON, handling cases where values might be objects instead of integers.
    /// </summary>
    /// <param name="jsonString">The JSON string to deserialize.</param>
    /// <param name="logContext">Optional context for logging (e.g., "breeding", "user-controller").</param>
    /// <returns>A dictionary with string keys and integer values.</returns>
    public static Dictionary<string, int> SafeDeserializeInventory(string? jsonString, string? logContext = null)
    {
        if (string.IsNullOrEmpty(jsonString))
        {
            return new Dictionary<string, int>();
        }

        try
        {
            // First try the expected format
            return JsonSerializer.Deserialize<Dictionary<string, int>>(jsonString) 
                   ?? new Dictionary<string, int>();
        }
        catch (JsonException)
        {
            try
            {
                // If that fails, try deserializing as a more flexible format
                using var document = JsonDocument.Parse(jsonString);
                var result = new Dictionary<string, int>();
                
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    var value = property.Value;
                    
                    // Handle different value types
                    switch (value.ValueKind)
                    {
                        case JsonValueKind.Number:
                            if (value.TryGetInt32(out var intValue))
                            {
                                result[property.Name] = intValue;
                            }
                            break;
                        case JsonValueKind.String:
                            if (int.TryParse(value.GetString(), out var parsedValue))
                            {
                                result[property.Name] = parsedValue;
                            }
                            break;
                        case JsonValueKind.Object:
                            // For objects, try to extract a count property or default to 1
                            if (value.TryGetProperty("count", out var countProperty) && 
                                countProperty.TryGetInt32(out var countValue))
                            {
                                result[property.Name] = countValue;
                            }
                            else
                            {
                                // Default to 1 for objects we can't parse
                                Log.Warning("Found object in inventory JSON for key '{Key}' in {Context}, defaulting to count 1", 
                                          property.Name, logContext ?? "unknown");
                                result[property.Name] = 1;
                            }
                            break;
                        default:
                            // Skip other types (arrays, booleans, etc.)
                            Log.Warning("Unexpected JSON value type '{ValueKind}' for inventory key '{Key}' in {Context}", 
                                      value.ValueKind, property.Name, logContext ?? "unknown");
                            break;
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to deserialize inventory JSON in {Context}: {Json}", 
                         logContext ?? "unknown", jsonString);
                return new Dictionary<string, int>();
            }
        }
    }

    /// <summary>
    /// Gets a value from the inventory with a default fallback, ensuring type safety.
    /// </summary>
    /// <param name="inventory">The inventory dictionary.</param>
    /// <param name="key">The item key to look up.</param>
    /// <param name="defaultValue">The default value if the key doesn't exist.</param>
    /// <returns>The item count or default value.</returns>
    public static int GetItemCount(Dictionary<string, int> inventory, string key, int defaultValue = 0)
    {
        return inventory?.GetValueOrDefault(key, defaultValue) ?? defaultValue;
    }

    /// <summary>
    /// Safely adds or updates an item count in the inventory.
    /// </summary>
    /// <param name="inventory">The inventory dictionary.</param>
    /// <param name="key">The item key.</param>
    /// <param name="amount">The amount to add (can be negative to subtract).</param>
    /// <returns>The new count for the item.</returns>
    public static int UpdateItemCount(Dictionary<string, int> inventory, string key, int amount)
    {
        if (inventory == null) throw new ArgumentNullException(nameof(inventory));
        
        var currentCount = inventory.GetValueOrDefault(key, 0);
        var newCount = Math.Max(0, currentCount + amount); // Prevent negative counts
        
        if (newCount == 0)
        {
            inventory.Remove(key);
        }
        else
        {
            inventory[key] = newCount;
        }
        
        return newCount;
    }

    /// <summary>
    /// Checks if the inventory has enough of a specific item.
    /// </summary>
    /// <param name="inventory">The inventory dictionary.</param>
    /// <param name="key">The item key.</param>
    /// <param name="requiredAmount">The required amount.</param>
    /// <returns>True if the inventory has enough of the item.</returns>
    public static bool HasItem(Dictionary<string, int> inventory, string key, int requiredAmount = 1)
    {
        return GetItemCount(inventory, key) >= requiredAmount;
    }
}