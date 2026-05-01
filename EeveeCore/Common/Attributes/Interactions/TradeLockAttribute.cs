using Discord.Interactions;
using EeveeCore.Modules.Trade.Services;

namespace EeveeCore.Common.Attributes.Interactions;

/// <summary>
///     Attribute that prevents a command from being run if the author is trade-locked,
///     and trade-locks the author for the entire runtime of the command.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TradeLockAttribute : PreconditionAttribute
{
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
        var tradeLockService = services.GetRequiredService<ITradeLockService>();

        if (await tradeLockService.IsUserTradeLockedAsync(context.User.Id))
        {
            var tradeService = services.GetService<TradeService>();
            if (tradeService != null)
            {
                var hasActiveSession = await HasActiveTradeSessionAsync(tradeService, context.User.Id);
                if (!hasActiveSession)
                {
                    await tradeService.ClearOrphanedTradeLocksAsync(context.User.Id);
                    return PreconditionResult.FromSuccess();
                }
            }
            
            return PreconditionResult.FromError($"{context.User.Username} is currently in a trade!");
        }

        return PreconditionResult.FromSuccess();
    }

    /// <summary>
    ///     Checks if a user has an active trade session.
    /// </summary>
    /// <param name="tradeService">The trade service to check with.</param>
    /// <param name="userId">The user ID to check.</param>
    /// <returns>True if the user has an active trade session, false otherwise.</returns>
    private static async Task<bool> HasActiveTradeSessionAsync(TradeService tradeService, ulong userId)
    {
        try
        {
            return await tradeService.HasActiveTradeSessionAsync(userId);
        }
        catch
        {
            return true;
        }
    }
}