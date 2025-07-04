using Discord.Interactions;
using EeveeCore.Common.Attributes.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Fishing.Services;
using Serilog;

namespace EeveeCore.Modules.Fishing;

/// <summary>
///     Slash commands for the fishing system.
/// </summary>
[Group("fish", "Fishing commands")]
public class FishingSlashCommands : EeveeCoreSlashModuleBase<FishingService>
{
    /// <summary>
    ///     Cast your fishing rod to catch Pokemon!
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("cast", "Cast your fishing rod to catch Pokemon!")]
    [TradeLock]
    public async Task FishAsync()
    {
        try
        {
            await DeferAsync();

            // Process fishing attempt
            var result = await Service.HandleFishing(Context);

            if (!result.Success)
            {
                await FollowupAsync(result.Message!, ephemeral: true);
                return;
            }

            // Send the fishing embed with image attachment
            var message = await SendFishingMessageAsync(result.ResponseEmbed!);

            // Store fishing data for event handling
            await Service.StoreFishingData(Context.Channel.Id, message.Id, result);

            // Handle multi-box chance if applicable
            if (result.ShowMultiBox)
            {
                await HandleMultiBoxAsync();
            }

            // Show energy warning if empty
            if (result.RemainingEnergy <= 0)
            {
                await FollowupAsync($"Sorry, you seem to be out of energy!\nVote for {Context.Client.CurrentUser.Username} to get more energy with `/ditto vote`!");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in fishing command for user {UserId}", Context.User.Id);
            await FollowupAsync("An error occurred while fishing. Please try again later.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Sends the fishing message with image attachment.
    /// </summary>
    /// <param name="embed">The embed to send.</param>
    /// <returns>The sent message.</returns>
    private async Task<IUserMessage> SendFishingMessageAsync(Embed embed)
    {
        var fishingImagePath = Path.Combine("data", "images", "fishing.gif");
        
        if (File.Exists(fishingImagePath))
        {
            try
            {
                await using var fileStream = new FileStream(fishingImagePath, FileMode.Open, FileAccess.Read);
                var fileAttachment = new FileAttachment(fileStream, "fishing.gif");
                return await FollowupWithFileAsync(embed: embed, attachment: fileAttachment);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to send fishing image attachment");
                // Fallback to embed without image
                var fallbackEmbed = embed.ToEmbedBuilder()
                    .WithImageUrl(null)
                    .WithDescription($"{embed.Description}\n\n*[Image not available]*")
                    .Build();
                return await FollowupAsync(embed: fallbackEmbed);
            }
        }
        else
        {
            Log.Warning("Fishing image not found at path: {ImagePath}", fishingImagePath);
            // Fallback to embed without image
            var fallbackEmbed = embed.ToEmbedBuilder()
                .WithImageUrl(null)
                .WithDescription($"{embed.Description}\n\n*[Image not available]*")
                .Build();
            return await FollowupAsync(embed: fallbackEmbed);
        }
    }

    /// <summary>
    ///     Handles the multi-box event.
    /// </summary>
    private async Task HandleMultiBoxAsync()
    {
        var boxMessage = await FollowupAsync("You fished up a box with question marks printed on all sides!");
        await Task.Delay(3000);

        var result = await Service.HandleMultiBox(Context.User.Id);
        await boxMessage.ModifyAsync(msg => msg.Content = result);
    }
}