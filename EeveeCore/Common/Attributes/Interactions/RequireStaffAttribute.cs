using Discord.Interactions;
using EeveeCore.Common.Enums;
using LinqToDB;

namespace EeveeCore.Common.Attributes.Interactions;

/// <summary>
///     Attribute to check if user has the required staff rank before executing a command or method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequireStaffAttribute : PreconditionAttribute
{
    private readonly StaffRank _requiredRank;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RequireStaffAttribute" /> class.
    /// </summary>
    /// <param name="requiredRank">The minimum staff rank required to execute the command.</param>
    public RequireStaffAttribute(StaffRank requiredRank)
    {
        _requiredRank = requiredRank;
    }

    /// <summary>
    ///     Checks the requirements before executing a command or method.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="command">The command being executed.</param>
    /// <param name="services">The service provider.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the precondition result.</returns>
    public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context,
        ICommandInfo command,
        IServiceProvider services)
    {
        var creds = services.GetRequiredService<BotCredentials>();
        var dbProvider = services.GetRequiredService<LinqToDbConnectionProvider>();

        // Check if user is owner first (bypass all checks)
        if (creds.IsOwner(context.User))
            return PreconditionResult.FromSuccess();

        // Get user's staff rank from database
        await using var db = await dbProvider.GetConnectionAsync();
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.UserId == context.User.Id);

        if (user == null)
            return PreconditionResult.FromError("User not found in database");

        // Parse staff rank from string
        if (!Enum.TryParse<StaffRank>(user.Staff, true, out var userRank))
            return PreconditionResult.FromError("Invalid staff rank");

        // Check if user has required rank or higher
        if (userRank >= _requiredRank)
            return PreconditionResult.FromSuccess();

        return PreconditionResult.FromError($"Insufficient permissions. Required: {_requiredRank}");
    }
}