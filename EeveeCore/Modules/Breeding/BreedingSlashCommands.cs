using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Breeding.Services;
using EeveeCore.Modules.Pokemon.Services;

namespace EeveeCore.Modules.Breeding;

/// <summary>
///     Module containing Pokémon breeding commands and interactions.
/// </summary>
[Group("breeding", "Commands for Pokémon breeding")]
public class BreedingModule(PokemonService pkServ) : EeveeCoreSlashModuleBase<BreedingService>
{
    private static readonly HashSet<ulong> AllowedUserIds = [790722073248661525];

    /// <summary>
    ///     Clears the user's breeding list of female IDs.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("clear", "Clears your breeding list of female IDs")]
    public async Task ClearBreedingList()
    {
        await Service.ClearUserFemalesAsync(ctx.User.Id);
        await RespondAsync("Cleared your female list.");
    }

    /// <summary>
    ///     Sets the list of female Pokémon IDs for breeding.
    /// </summary>
    /// <param name="femaleIds">Space or comma-separated list of female Pokémon IDs.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("setfemales", "Set your female Pokémon IDs. Accepts space or comma-separated values")]
    public async Task SetFemales(string femaleIds)
    {
        await DeferAsync();
        // Determine the delimiter
        var delimiter = femaleIds.Contains(',') ? ',' : ' ';

        // Split and clean the IDs
        var idStrings = femaleIds.Split(delimiter)
            .Select(id => id.Trim())
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        // Validate IDs
        var validatedIds = new List<ulong>();
        var invalidIds = new List<string>();

        foreach (var idStr in idStrings)
        {
            if (!ulong.TryParse(idStr, out var id))
            {
                invalidIds.Add(idStr);
                continue;
            }

            // Check if the Pokémon exists and is female or Ditto
            var isValid = await Service.ValidateFemaleIdAsync(ctx.User.Id, id);

            if (isValid)
                validatedIds.Add(id);
            else
                invalidIds.Add(idStr);
        }

        // Update the user's females list
        if (validatedIds.Any()) await Service.UpdateUserFemalesAsync(ctx.User.Id, validatedIds);

        // Build response message
        var responseMessage =
            $"Your female Pokémon list has been updated with valid IDs: {string.Join(", ", validatedIds)}";

        if (invalidIds.Any())
            responseMessage += $"\nInvalid or non-female IDs detected: {string.Join(", ", invalidIds)}.";

        // Send as an embedded response
        await ctx.Interaction.SendConfirmFollowupAsync(responseMessage);
    }

    /// <summary>
    ///     Breeds a male Pokémon with the first female Pokémon in the user's breeding list.
    /// </summary>
    /// <param name="maleId">The ID of the male Pokémon to breed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("breed", "Breed a male pokemon with a female from your breeding list")]
    public async Task Breed([Summary("male_id", "ID of the male Pokémon")] int maleId)
    {
        await DeferAsync();
        var message = await ctx.Interaction.SendConfirmFollowupAsync("Breeding in progress...");
        await BreedPokemon((ulong)maleId, ctx.Interaction);
    }

    /// <summary>
    ///     Core breeding implementation that handles the breeding process.
    /// </summary>
    /// <param name="male">The ID of the male Pokémon to breed.</param>
    /// <param name="interaction">The message to update with breeding results.</param>
    /// <param name="auto">Whether this is an auto-retry.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task BreedPokemon(ulong male, IDiscordInteraction interaction, bool auto = false)
    {
        // Get the first female from the user's breeding list
        var femaleId = await Service.FetchFirstFemaleAsync(ctx.User.Id);
        if (femaleId == null)
        {
            var eb = new EmbedBuilder()
                .WithErrorColor()
                .WithDescription(
                    "You must set your list of female pokemon ID's with `/breeding setfemales` before using this command!")
                .Build();
            await interaction.ModifyOriginalResponseAsync(
                x => x.Embed = eb);
            return;
        }

        // Attempt to breed
        var result = await Service.AttemptBreedAsync(ctx.User.Id, male, femaleId.Value);

        // If there was an error, display it and return
        if (!result.Success)
        {
            var embed = new EmbedBuilder()
                .WithTitle("Breeding Attempt Failed!")
                .WithDescription(
                    $"{result.ErrorMessage}\nYou can breed again: <t:{DateTimeOffset.UtcNow.AddSeconds(50).ToUnixTimeSeconds()}:R>")
                .WithColor(Color.Red);

            if (result.Chance > 0) embed.WithFooter($"Chance of success: {result.Chance * 100:F2}%");

            // Create components based on whether this is an auto-retry
            var components = CreateFailureComponents((int)male, auto);

            await interaction.ModifyOriginalResponseAsync(m =>
            {
                m.Embed = embed.Build();
                m.Components = components.Build();
            });

            // Handle auto-retry if enabled
            if (auto && Service.GetAutoBreedState(ctx.User.Id) == (int?)male)
            {
                var retryCount = Service.GetBreedRetries(ctx.User.Id, (int)male);

                if (retryCount < 15) // Limit to 15 retries
                {
                    var newRetryCount = Service.IncrementBreedRetries(ctx.User.Id, (int)male);

                    var retryEmbed = new EmbedBuilder()
                        .WithTitle("Breeding Attempt Failed!")
                        .WithDescription(
                            $"{result.ErrorMessage}\n\n`Auto-retry attempts:` **{newRetryCount}**\n`(max 15)`")
                        .WithColor(Color.Red);

                    if (result.Chance > 0) retryEmbed.WithFooter($"Chance of success: {result.Chance * 100:F2}%");

                    await interaction.ModifyOriginalResponseAsync(m => { m.Embed = retryEmbed.Build(); });

                    // Check if we hit cooldown
                    if (result.ErrorMessage.Contains("Command on cooldown for"))
                    {
                        // Extract cooldown time from error message
                        var cooldownSeconds = ExtractCooldownTime(result.ErrorMessage);
                        if (cooldownSeconds > 0)
                        {
                            var cooldownMs = (cooldownSeconds + 1) * 1000; // Add 1 second buffer
                            retryEmbed.WithDescription(
                                $"{result.ErrorMessage}\n\nWaiting for cooldown...\n`Auto-retry attempts:` **{newRetryCount}**\n`(max 15)`");
                            await interaction.ModifyOriginalResponseAsync(m => { m.Embed = retryEmbed.Build(); });

                            // Wait for cooldown to expire
                            await Task.Delay(cooldownMs);
                        }
                        else
                        {
                            // Default wait of 36 seconds if we can't extract time
                            await Task.Delay(36000);
                        }
                    }
                    else
                    {
                        // Small delay for other errors
                        await Task.Delay(500);
                    }

                    // Try again after waiting
                    await BreedPokemon(male, interaction, true);
                    return;
                }

                var limitEmbed = new EmbedBuilder()
                    .WithTitle("Auto-breed retry limit reached!!")
                    .WithDescription(
                        $"Please use the breed command to try again.\n`Auto-retry attempts:` **{retryCount}**\n`(max 15)`")
                    .WithColor(Color.Red);

                if (result.Chance > 0) limitEmbed.WithFooter($"Chance of success: {result.Chance * 100:F2}%");

                await interaction.ModifyOriginalResponseAsync(m =>
                {
                    m.Embed = limitEmbed.Build();
                    m.Components = new ComponentBuilder().Build();
                });
                await ctx.Channel.SendMessageAsync(ctx.User.Mention);

                // Reset retry counter
                Service.ResetBreedRetries(ctx.User.Id, (int)male);
                return;
            }

            return;
        }

        // Reset auto-breeding state
        Service.SetAutoBreedState(ctx.User.Id, null);

        // Get parent names for the image
        var parentNames = await Service.GetParentNamesAsync(ctx.User.Id, male, femaleId.Value);

        // Generate success image
        var imageData = await Service.CreateSuccessImageAsync(
            result,
            parentNames.FatherName,
            parentNames.MotherName
        );

        var file = new FileAttachment(new MemoryStream(imageData), "image.png");

        // Create success embed
        var successEmbed = new EmbedBuilder()
            .WithTitle("Success!")
            .WithDescription($"It will hatch after {result.Counter} *counted* messages!\n" +
                             $"Your {parentNames.MotherName} will be on breeding cooldown for 6 Hours!\n\n" +
                             $"You can breed again in <t:{DateTimeOffset.UtcNow.AddSeconds(50).ToUnixTimeSeconds()}:R>")
            .WithImageUrl("attachment://image.png")
            .WithFooter($"Chance of success: {result.Chance * 100:F2}%");

        // Set color based on result
        if (result.IsShadow)
            successEmbed.WithColor(new Color(0x4f0fff));
        else if (result.IsShiny)
            successEmbed.WithColor(new Color(0xffeb0f));
        else
            successEmbed.WithColor(new Color(0x0fff13));

        // Remove the first female from the list if not a Ditto
        if (result.Child.Name.ToLower() != "ditto") await Service.RemoveFirstFemaleAsync(ctx.User.Id);

        else
            await interaction.ModifyOriginalResponseAsync(m =>
            {
                m.Content = ctx.User.Mention;
                m.Embed = successEmbed.Build();
                m.Components = new ComponentBuilder().Build();
                m.Attachments = new[] { file };
            });

        // Reset retry counter
        Service.ResetBreedRetries(ctx.User.Id, (int)male);
    }

    private static int ExtractCooldownTime(string errorMessage)
    {
        try
        {
            // Extract number from "Command on cooldown for Xs"
            var match = System.Text.RegularExpressions.Regex.Match(errorMessage, @"cooldown for (\d+)s");
            if (match is { Success: true, Groups.Count: > 1 })
            {
                return int.Parse(match.Groups[1].Value);
            }
        }
        catch
        {
            // If parsing fails, return 0
        }
        return 0;
    }

    /// <summary>
    ///     Creates the component buttons for a breeding failure message.
    /// </summary>
    /// <param name="maleId">The ID of the male Pokémon being bred.</param>
    /// <param name="auto">Whether this is an auto-retry.</param>
    /// <returns>A ComponentBuilder containing the appropriate buttons.</returns>
    private ComponentBuilder CreateFailureComponents(int maleId, bool auto)
    {
        var components = new ComponentBuilder();

        if (auto)
            components.WithButton("Cancel auto breed", $"cancel_auto_breed_{maleId}", ButtonStyle.Danger,
                new Emoji("❎"));
        else
            components.WithButton("Redo breed", $"redo_breed_{maleId}", ButtonStyle.Secondary, new Emoji("✅"))
                .WithButton("Auto redo until success", $"auto_redo_{maleId}", ButtonStyle.Primary, new Emoji("🔄"));

        return components;
    }


    /// <summary>
    ///     Handler for the redo breed button interaction.
    /// </summary>
    /// <param name="maleIdStr">The string ID of the male Pokémon to breed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("redo_breed_*", true)]
    public async Task RedoBreedHandler(string maleIdStr)
    {
        if (!int.TryParse(maleIdStr, out var maleId))
        {
            await RespondAsync("Invalid Pokémon ID.", ephemeral: true);
            return;
        }

        // Verify user
        if (Context.User.Id != ctx.User.Id && !AllowedUserIds.Contains(Context.User.Id))
        {
            await RespondAsync("You are not allowed to interact with this button.", ephemeral: true);
            return;
        }

        await DeferAsync();

        // Rerun the breeding process
        await BreedPokemon((ulong)maleId, ctx.Interaction);
    }

    /// <summary>
    ///     Handler for the auto-redo button interaction.
    /// </summary>
    /// <param name="maleIdStr">The string ID of the male Pokémon to breed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("auto_redo_*", true)]
    public async Task AutoRedoHandler(string maleIdStr)
    {
        if (!int.TryParse(maleIdStr, out var maleId))
        {
            await RespondAsync("Invalid Pokémon ID.", ephemeral: true);
            return;
        }

        // Verify user
        if (Context.User.Id != ctx.User.Id && !AllowedUserIds.Contains(Context.User.Id))
        {
            await RespondAsync("You are not allowed to interact with this button.", ephemeral: true);
            return;
        }

        await DeferAsync();

        // Check if already auto-breeding
        var currentAutoBreed = Service.GetAutoBreedState(ctx.User.Id);
        if (currentAutoBreed != null)
        {
            var cancelComponents = new ComponentBuilder()
                .WithButton("Cancel auto breed", $"cancel_auto_breed_{maleId}", ButtonStyle.Danger, new Emoji("❎"))
                .Build();

            await FollowupAsync(
                "You already have an active auto-breed. Cancel that one first!",
                components: cancelComponents,
                ephemeral: true
            );
            return;
        }


        await FollowupAsync("I will attempt to breed these pokes until the breed is successful!", ephemeral: true);

        // Set auto-breeding state
        Service.SetAutoBreedState(ctx.User.Id, maleId);


        // Wait and attempt to breed
        await Task.Delay(37000); // 37 seconds

        if (Service.GetAutoBreedState(ctx.User.Id) == maleId)
            // Invoke the breed method with auto=true
            await BreedPokemon((ulong)maleId, ctx.Interaction, true);
    }

    /// <summary>
    ///     Handler for the cancel auto-breed button interaction.
    /// </summary>
    /// <param name="maleIdStr">The string ID of the male Pokémon being bred.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("cancel_auto_breed_*", true)]
    public async Task CancelAutoBreedHandler(string maleIdStr)
    {
        // Verify user
        if (Context.User.Id != ctx.User.Id && !AllowedUserIds.Contains(Context.User.Id))
        {
            await RespondAsync("You are not allowed to interact with this button.", ephemeral: true);
            return;
        }

        // Cancel auto-breeding
        Service.SetAutoBreedState(ctx.User.Id, null);

        await RespondAsync("I will no longer automatically attempt to breed these pokes.", ephemeral: true);
    }
}