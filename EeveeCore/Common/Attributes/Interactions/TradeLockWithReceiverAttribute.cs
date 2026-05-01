using Discord.Interactions;

namespace EeveeCore.Common.Attributes.Interactions;

/// <summary>
///     Attribute that prevents a command from being run if the author OR the first IUser parameter is trade-locked,
///     and trade-locks both the author AND the first IUser parameter for the entire runtime of the command.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TradeLockWithReceiverAttribute : PreconditionAttribute
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
            return PreconditionResult.FromError($"{context.User.Username} is currently in a trade!");

        return PreconditionResult.FromSuccess();
    }
}