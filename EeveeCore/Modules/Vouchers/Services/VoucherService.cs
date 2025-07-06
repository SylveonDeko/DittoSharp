using EeveeCore.Database;
using EeveeCore.Modules.Vouchers.Common;
using EeveeCore.Modules.Vouchers.Models;
using EeveeCore.Services;
using LinqToDB;
using Serilog;

namespace EeveeCore.Modules.Vouchers.Services;

/// <summary>
///     Service for handling voucher request operations.
///     Manages creation, retrieval, and status updates of voucher requests.
/// </summary>
/// <param name="dbProvider">The database connection provider.</param>
public class VoucherService(LinqToDbConnectionProvider dbProvider) : INService
{
    /// <summary>
    ///     Creates a new voucher request and inserts it into the database.
    /// </summary>
    /// <param name="userId">The Discord user ID of the requester.</param>
    /// <param name="formData">The completed voucher request form data.</param>
    /// <param name="messageId">The Discord message ID associated with the request.</param>
    /// <param name="channelId">The Discord channel ID where the request was posted.</param>
    /// <returns>The created voucher request.</returns>
    public async Task<VoucherRequest> CreateVoucherRequestAsync(ulong userId, VoucherRequestFormData formData, ulong? messageId = null, ulong? channelId = null)
    {
        try
        {
            var request = new VoucherRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                UserId = userId,
                Pokemon = formData.Pokemon,
                Appearance = formData.Appearance,
                PaymentMethod = formData.PaymentMethod,
                Artist = formData.Artist,
                Status = new List<string> { "Created" },
                CreatedAt = DateTimeOffset.UtcNow,
                MessageId = messageId,
                ChannelId = channelId
            };

            await using var db = await dbProvider.GetConnectionAsync();
            
            // Insert into voucher_requests table
            await db.GetTable<Database.Linq.Models.Bot.VoucherRequest>()
                .InsertAsync(() => new Database.Linq.Models.Bot.VoucherRequest
                {
                    MessageId = messageId ?? 0,
                    UserId = userId,
                    Status = request.Status.ToArray(),
                    ArtistId = request.Artist
                });

            return request;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error creating voucher request for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    ///     Retrieves all voucher requests for a specific user.
    /// </summary>
    /// <param name="userId">The Discord user ID to get requests for.</param>
    /// <returns>A list of voucher requests for the user.</returns>
    public async Task<List<VoucherRequest>> GetUserVoucherRequestsAsync(ulong userId)
    {
        try
        {
            await using var db = await dbProvider.GetConnectionAsync();
            
            var dbRequests = await db.GetTable<Database.Linq.Models.Bot.VoucherRequest>()
                .Where(r => r.UserId == userId)
                .ToListAsync();

            return dbRequests.Select(r => new VoucherRequest
            {
                RequestId = r.MessageId.ToString() ?? "0",
                UserId = r.UserId,
                Status = r.Status?.ToList() ?? new List<string> { "Created" },
                Artist = r.ArtistId.GetValueOrDefault(),
                MessageId = r.MessageId,
                CreatedAt = DateTimeOffset.UtcNow // Note: We don't have created_at in the DB model
            }).ToList();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error retrieving voucher requests for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    ///     Updates the status of a voucher request.
    /// </summary>
    /// <param name="messageId">The Discord message ID of the request.</param>
    /// <param name="newStatuses">The new status values to set.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    public async Task<bool> UpdateVoucherRequestStatusAsync(ulong messageId, List<string> newStatuses)
    {
        try
        {
            await using var db = await dbProvider.GetConnectionAsync();
            
            var rowsAffected = await db.GetTable<Database.Linq.Models.Bot.VoucherRequest>()
                .Where(r => r.MessageId == messageId)
                .Set(r => r.Status, newStatuses.ToArray())
                .UpdateAsync();

            return rowsAffected > 0;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error updating voucher request status for message {MessageId}", messageId);
            throw;
        }
    }

    /// <summary>
    ///     Checks if a user has reached the maximum number of active voucher requests.
    /// </summary>
    /// <param name="userId">The Discord user ID to check.</param>
    /// <returns>True if the user can create more requests, false otherwise.</returns>
    public async Task<bool> CanUserCreateMoreRequestsAsync(ulong userId)
    {
        try
        {
            await using var db = await dbProvider.GetConnectionAsync();
            
            var activeRequestCount = await db.GetTable<Database.Linq.Models.Bot.VoucherRequest>()
                .Where(r => r.UserId == userId)
                .CountAsync();

            return activeRequestCount < VoucherConstants.MaxRequestsPerUser;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error checking voucher request limit for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    ///     Checks if a user has any available vouchers to spend.
    /// </summary>
    /// <param name="userId">The Discord user ID to check.</param>
    /// <returns>True if the user has vouchers available, false otherwise.</returns>
    public async Task<bool> HasAvailableVouchersAsync(ulong userId)
    {
        try
        {
            await using var db = await dbProvider.GetConnectionAsync();
            
            var user = await db.GetTable<Database.Linq.Models.Bot.User>()
                .Where(u => u.UserId == userId)
                .FirstOrDefaultAsync();

            return user?.Voucher > 0;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error checking voucher count for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    ///     Formats the status list for display purposes.
    /// </summary>
    /// <param name="statuses">The list of status values.</param>
    /// <returns>A formatted string representation of the statuses.</returns>
    public static string FormatStatusList(List<string> statuses)
    {
        if (statuses == null || statuses.Count == 0)
            return "No status";

        return string.Join(", ", statuses);
    }

    /// <summary>
    ///     Gets the appropriate color for a voucher request based on its current status.
    /// </summary>
    /// <param name="statuses">The list of status values.</param>
    /// <returns>A Discord color representing the current status.</returns>
    public static Color GetStatusColor(List<string> statuses)
    {
        if (statuses == null || statuses.Count == 0)
            return Color.LightGrey;

        // Use the most recent/important status for color
        var primaryStatus = statuses.LastOrDefault() ?? "Created";
        
        return VoucherConstants.StatusColors.TryGetValue(primaryStatus, out var color) 
            ? color 
            : Color.LightGrey;
    }
}