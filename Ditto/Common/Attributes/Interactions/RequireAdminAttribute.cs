using Discord.Interactions;

namespace Ditto.Common.Attributes.Interactions;

/// <summary>
///     Attribute to check user permissions before executing a command or method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequireAdminAttribute : PreconditionAttribute
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
        var creds = services.GetRequiredService<BotCredentials>();
        var isOwner = creds.IsOwner(context.User);
        return isOwner ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Not owner");
    }
}