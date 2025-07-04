using EeveeCore.Database.Linq.Models.Bot;
using EeveeCore.Services.Impl;
using LinqToDB;
using LinqToDB.Data;
using Serilog;

namespace EeveeCore.Services;

/// <summary>
///     Service for managing user-defined filter groups for Pokemon collections.
///     Provides CRUD operations and validation for dynamic filter configurations.
/// </summary>
public class FilterGroupService(LinqToDbConnectionProvider dbProvider, RedisCache redis) : INService
{
    private const string CacheKeyPrefix = "filter_groups:";
    private const int CacheExpiryMinutes = 30;

    /// <summary>
    ///     Gets all filter groups for a user.
    /// </summary>
    /// <param name="userId">The Discord user ID.</param>
    /// <returns>List of filter groups ordered by sort order and favorites first.</returns>
    public async Task<List<UserFilterGroup>> GetUserFilterGroups(ulong userId)
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}user:{userId}";
            var cached = await redis.GetFromCache<List<UserFilterGroup>>(cacheKey);
            if (cached != null)
                return cached;

            await using var db = await dbProvider.GetConnectionAsync();
            
            // Get filter groups with criteria loaded
            var filterGroups = await db.GetTable<UserFilterGroup>()
                .LoadWithAsTable(g => g.FilterCriteria)
                .Where(g => g.UserId == userId && g.IsActive)
                .OrderByDescending(g => g.IsFavorite)
                .ThenBy(g => g.SortOrder)
                .ThenBy(g => g.Name)
                .ToListAsync();


            await redis.AddToCache(cacheKey, filterGroups, TimeSpan.FromMinutes(CacheExpiryMinutes));
            return filterGroups;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving filter groups for user {UserId}", userId);
            return new List<UserFilterGroup>();
        }
    }

    /// <summary>
    ///     Gets a specific filter group by ID with ownership validation.
    /// </summary>
    /// <param name="groupId">The filter group ID.</param>
    /// <param name="userId">The Discord user ID for ownership validation.</param>
    /// <returns>The filter group or null if not found or not owned by user.</returns>
    public async Task<UserFilterGroup?> GetFilterGroup(int groupId, ulong userId)
    {
        try
        {
            await using var db = await dbProvider.GetConnectionAsync();
            
            // Get the filter group with criteria loaded
            var filterGroup = await db.GetTable<UserFilterGroup>()
                .LoadWithAsTable(g => g.FilterCriteria)
                .FirstOrDefaultAsync(g => g.Id == groupId && g.UserId == userId);
                
            if (filterGroup == null)
                return null;
                
                
            return filterGroup;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving filter group {GroupId} for user {UserId}", groupId, userId);
            return null;
        }
    }

    /// <summary>
    ///     Creates a new filter group for a user.
    /// </summary>
    /// <param name="userId">The Discord user ID.</param>
    /// <param name="name">The name of the filter group.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="color">Optional hex color code.</param>
    /// <param name="icon">Optional emoji or icon.</param>
    /// <param name="criteria">List of filter criteria.</param>
    /// <returns>The created filter group or null if creation failed.</returns>
    public async Task<UserFilterGroup?> CreateFilterGroup(
        ulong userId,
        string name,
        string? description = null,
        string? color = null,
        string? icon = null,
        List<UserFilterCriteria>? criteria = null)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
                return null;

            if (description?.Length > 500)
                return null;

            if (color != null && (!color.StartsWith('#') || color.Length != 7))
                return null;

            await using var db = await dbProvider.GetConnectionAsync();
            await using var transaction = await db.BeginTransactionAsync();

            try
            {
                // Check if user already has a group with this name
                var existingGroup = await db.GetTable<UserFilterGroup>()
                    .AnyAsync(g => g.UserId == userId && g.Name == name && g.IsActive);

                if (existingGroup)
                    return null;

                // Get next sort order
                var maxSortOrder = await db.GetTable<UserFilterGroup>()
                    .Where(g => g.UserId == userId)
                    .MaxAsync(g => (int?)g.SortOrder) ?? -1;

                var filterGroup = new UserFilterGroup
                {
                    UserId = userId,
                    Name = name,
                    Description = description,
                    Color = color ?? "#3B82F6",
                    Icon = icon ?? "📁",
                    SortOrder = maxSortOrder + 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Insert filter group and get the ID
                filterGroup.Id = await db.InsertWithInt32IdentityAsync(filterGroup);

                // Add criteria if provided
                if (criteria != null && criteria.Count > 0)
                {
                    for (var i = 0; i < criteria.Count; i++)
                    {
                        var criterion = criteria[i];
                        criterion.FilterGroupId = filterGroup.Id;
                        criterion.CriterionOrder = i;
                        
                        // Validate criterion
                        if (!IsValidCriterion(criterion))
                            throw new ArgumentException($"Invalid criterion at index {i}");
                    }

                    await db.BulkCopyAsync(criteria);
                }

                await transaction.CommitAsync();

                // Clear cache
                await InvalidateUserCache(userId);

                // Return with criteria loaded
                return await GetFilterGroup(filterGroup.Id, userId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating filter group for user {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    ///     Updates an existing filter group.
    /// </summary>
    /// <param name="groupId">The filter group ID.</param>
    /// <param name="userId">The Discord user ID for ownership validation.</param>
    /// <param name="name">New name (optional).</param>
    /// <param name="description">New description (optional).</param>
    /// <param name="color">New color (optional).</param>
    /// <param name="icon">New icon (optional).</param>
    /// <param name="isFavorite">New favorite status (optional).</param>
    /// <param name="criteria">New criteria list (optional, replaces all existing).</param>
    /// <returns>True if updated successfully.</returns>
    public async Task<bool> UpdateFilterGroup(
        int groupId,
        ulong userId,
        string? name = null,
        string? description = null,
        string? color = null,
        string? icon = null,
        bool? isFavorite = null,
        List<UserFilterCriteria>? criteria = null)
    {
        try
        {
            await using var db = await dbProvider.GetConnectionAsync();
            await using var transaction = await db.BeginTransactionAsync();

            try
            {
                var filterGroup = await db.GetTable<UserFilterGroup>()
                    .LoadWithAsTable(g => g.FilterCriteria)
                    .FirstOrDefaultAsync(g => g.Id == groupId && g.UserId == userId);

                if (filterGroup == null)
                    return false;

                // Update properties if provided
                if (name != null)
                {
                    if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
                        return false;
                    
                    // Check for duplicate name
                    var duplicateName = await db.GetTable<UserFilterGroup>()
                        .AnyAsync(g => g.UserId == userId && g.Name == name && g.Id != groupId && g.IsActive);
                    
                    if (duplicateName)
                        return false;

                    filterGroup.Name = name;
                }

                if (description != null)
                {
                    if (description.Length > 500)
                        return false;
                    filterGroup.Description = description;
                }

                if (color != null)
                {
                    if (!color.StartsWith('#') || color.Length != 7)
                        return false;
                    filterGroup.Color = color;
                }

                if (icon != null)
                    filterGroup.Icon = icon;

                if (isFavorite.HasValue)
                    filterGroup.IsFavorite = isFavorite.Value;

                // Update the filter group
                await db.GetTable<UserFilterGroup>()
                    .Where(g => g.Id == groupId)
                    .Set(g => g.Name, filterGroup.Name!)
                    .Set(g => g.Description, filterGroup.Description)
                    .Set(g => g.Color, filterGroup.Color)
                    .Set(g => g.Icon, filterGroup.Icon)
                    .Set(g => g.IsFavorite, filterGroup.IsFavorite)
                    .Set(g => g.UpdatedAt, DateTime.UtcNow)
                    .UpdateAsync();

                // Update criteria if provided
                if (criteria != null)
                {
                    // Remove existing criteria
                    await db.UserFilterCriteria
                        .Where(c => c.FilterGroupId == groupId)
                        .DeleteAsync();

                    // Add new criteria
                    for (var i = 0; i < criteria.Count; i++)
                    {
                        var criterion = criteria[i];
                        criterion.FilterGroupId = groupId;
                        criterion.CriterionOrder = i;
                        
                        if (!IsValidCriterion(criterion))
                            throw new ArgumentException($"Invalid criterion at index {i}");
                    }

                    if (criteria.Count > 0)
                        await db.BulkCopyAsync(criteria);
                }
                await transaction.CommitAsync();

                // Clear cache
                await InvalidateUserCache(userId);

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating filter group {GroupId} for user {UserId}", groupId, userId);
            return false;
        }
    }

    /// <summary>
    ///     Deletes a filter group.
    /// </summary>
    /// <param name="groupId">The filter group ID.</param>
    /// <param name="userId">The Discord user ID for ownership validation.</param>
    /// <returns>True if deleted successfully.</returns>
    public async Task<bool> DeleteFilterGroup(int groupId, ulong userId)
    {
        try
        {
            await using var db = await dbProvider.GetConnectionAsync();
            
            var deleted = await db.GetTable<UserFilterGroup>()
                .Where(g => g.Id == groupId && g.UserId == userId)
                .Set(g => g.IsActive, false)
                .Set(g => g.UpdatedAt, DateTime.UtcNow)
                .UpdateAsync();

            if (deleted > 0)
            {
                await InvalidateUserCache(userId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting filter group {GroupId} for user {UserId}", groupId, userId);
            return false;
        }
    }

    /// <summary>
    ///     Updates the sort order of filter groups for a user.
    /// </summary>
    /// <param name="userId">The Discord user ID.</param>
    /// <param name="groupIds">List of group IDs in new sort order.</param>
    /// <returns>True if reordered successfully.</returns>
    public async Task<bool> ReorderFilterGroups(ulong userId, List<int> groupIds)
    {
        try
        {
            await using var db = await dbProvider.GetConnectionAsync();
            await using var transaction = await db.BeginTransactionAsync();

            try
            {
                // Verify all groups belong to the user
                var userGroups = await db.GetTable<UserFilterGroup>()
                    .Where(g => g.UserId == userId && groupIds.Contains(g.Id))
                    .ToListAsync();

                if (userGroups.Count != groupIds.Count)
                    return false;

                // Update sort orders
                for (var i = 0; i < groupIds.Count; i++)
                {
                    var groupId = groupIds[i];
                    await db.GetTable<UserFilterGroup>()
                        .Where(g => g.Id == groupId)
                        .Set(g => g.SortOrder, i)
                        .Set(g => g.UpdatedAt, DateTime.UtcNow)
                        .UpdateAsync();
                }
                await transaction.CommitAsync();

                await InvalidateUserCache(userId);
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reordering filter groups for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    ///     Validates a filter criterion.
    /// </summary>
    /// <param name="criterion">The criterion to validate.</param>
    /// <returns>True if valid.</returns>
    private static bool IsValidCriterion(UserFilterCriteria criterion)
    {
        if (string.IsNullOrWhiteSpace(criterion.FieldName) || criterion.FieldName.Length > 50)
            return false;

        if (string.IsNullOrWhiteSpace(criterion.Operator) || criterion.Operator.Length > 20)
            return false;

        // Validate field names against known Pokemon properties
        var validFields = new HashSet<string>
        {
            "level", "hp_iv", "attack_iv", "defense_iv", "special_attack_iv", "special_defense_iv", "speed_iv",
            "iv_total", "iv_percentage", "shiny", "radiant", "nature", "gender", "type", "caught_at",
            "favorite", "champion", "tradable", "breedable", "pokemon_name", "nickname", "held_item",
            "happiness", "experience", "tags", "moves", "skin", "market_enlist"
        };

        if (!validFields.Contains(criterion.FieldName.ToLower()))
            return false;

        // Validate operators
        var validOperators = new HashSet<string>
        {
            "equals", "not_equals", "greater_than", "less_than", "greater_equal", "less_equal",
            "between", "contains", "not_contains", "in_list", "not_in_list", "is_null", "is_not_null"
        };

        if (!validOperators.Contains(criterion.Operator.ToLower()))
            return false;

        // Validate logical connectors
        if (criterion.LogicalConnector != null)
        {
            var validConnectors = new HashSet<string> { "AND", "OR" };
            if (!validConnectors.Contains(criterion.LogicalConnector.ToUpper()))
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Clears the cache for a user's filter groups.
    /// </summary>
    /// <param name="userId">The Discord user ID.</param>
    private async Task InvalidateUserCache(ulong userId)
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}user:{userId}";
            await redis.RemoveFromCache(cacheKey);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to invalidate cache for user {UserId}", userId);
        }
    }
}