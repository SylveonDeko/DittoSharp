using Discord.Interactions;
using EeveeCore.Common.Attributes.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Trade.Models;
using EeveeCore.Modules.Trade.Services;

namespace EeveeCore.Modules.Trade;

/// <summary>
///     Provides Discord slash commands for gifting functionality.
///     Handles gifting credits, redeems, Pokemon, and tokens between users.
/// </summary>
[Group("gift", "Gift items to other users")]
public class GiftSlashCommands : EeveeCoreSlashModuleBase<GiftService>
{
    /// <summary>
    ///     Gifts credits to another user.
    /// </summary>
    /// <param name="user">The user to gift credits to.</param>
    /// <param name="amount">The amount of credits to gift.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("credits", "Gift credits to another user")]
    [TradeLock]
    public async Task GiftCredits(
        [Summary("user", "The user to gift credits to")]
        IUser user,
        [Summary("amount", "The amount of credits to gift")]
        long amount)
    {
        if (amount <= 0)
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Invalid Amount")
                .WithDescription("You need to give at least 1 credit!")
                .WithColor(Color.Red)
                .Build();

            await RespondAsync(embed: errorEmbed, ephemeral: true);
            return;
        }

        if (ctx.User.Id == user.Id)
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Cannot Gift Yourself")
                .WithDescription("You cannot give yourself credits.")
                .WithColor(Color.Red)
                .Build();

            await RespondAsync(embed: errorEmbed, ephemeral: true);
            return;
        }

        // Show confirmation dialog
        var confirmEmbed = new EmbedBuilder()
            .WithTitle("🎁 Confirm Gift")
            .WithDescription($"Are you sure you want to give **{amount:N0}** credits to {user.Mention}?")
            .WithColor(Color.Orange)
            .WithFooter("This action cannot be undone.");

        if (!await PromptUserConfirmAsync(confirmEmbed, ctx.User.Id))
        {
            var cancelEmbed = new EmbedBuilder()
                .WithTitle("❌ Gift Cancelled")
                .WithDescription("The gift has been cancelled.")
                .WithColor(Color.Red)
                .Build();

            await FollowupAsync(embed: cancelEmbed, ephemeral: true);
            return;
        }

        var result = await Service.GiftCreditsAsync(ctx.User.Id, user.Id, (ulong)amount);

        if (result.Success)
        {
            var successEmbed = new EmbedBuilder()
                .WithTitle("🎉 Gift Successful!")
                .WithDescription($"{ctx.User.Mention} has gifted **{amount:N0}** credits to {user.Mention}!")
                .WithColor(Color.Green)
                .WithThumbnailUrl("https://cdn.discordapp.com/emojis/1010679749212901407.png") // MewCoin emoji
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await FollowupAsync(embed: successEmbed);
        }
        else
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Gift Failed")
                .WithDescription(result.Message)
                .WithColor(Color.Red)
                .Build();

            await FollowupAsync(embed: errorEmbed, ephemeral: true);
        }
    }

    /// <summary>
    ///     Gifts redeems to another user.
    /// </summary>
    /// <param name="user">The user to gift redeems to.</param>
    /// <param name="amount">The amount of redeems to gift.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("redeems", "Gift redeems to another user")]
    [TradeLock]
    public async Task GiftRedeems(
        [Summary("user", "The user to gift redeems to")]
        IUser user,
        [Summary("amount", "The amount of redeems to gift")]
        int amount)
    {
        if (amount <= 0)
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Invalid Amount")
                .WithDescription("You need to give at least 1 redeem!")
                .WithColor(Color.Red)
                .Build();

            await RespondAsync(embed: errorEmbed, ephemeral: true);
            return;
        }

        if (ctx.User.Id == user.Id)
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Cannot Gift Yourself")
                .WithDescription("You cannot give yourself redeems.")
                .WithColor(Color.Red)
                .Build();

            await RespondAsync(embed: errorEmbed, ephemeral: true);
            return;
        }

        // Show confirmation dialog
        var confirmEmbed = new EmbedBuilder()
            .WithTitle("🎁 Confirm Gift")
            .WithDescription($"Are you sure you want to give **{amount}** redeems to {user.Mention}?")
            .WithColor(Color.Orange)
            .WithFooter("This action cannot be undone.");

        if (!await PromptUserConfirmAsync(confirmEmbed, ctx.User.Id))
        {
            var cancelEmbed = new EmbedBuilder()
                .WithTitle("❌ Gift Cancelled")
                .WithDescription("The gift has been cancelled.")
                .WithColor(Color.Red)
                .Build();

            await FollowupAsync(embed: cancelEmbed, ephemeral: true);
            return;
        }

        var result = await Service.GiftRedeemsAsync(ctx.User.Id, user.Id, amount);

        if (result.Success)
        {
            var successEmbed = new EmbedBuilder()
                .WithTitle("🎉 Gift Successful!")
                .WithDescription($"{ctx.User.Mention} has gifted **{amount}** redeems to {user.Mention}!")
                .WithColor(Color.Green)
                .WithThumbnailUrl(
                    "https://cdn.discordapp.com/emojis/1008748071584616569.png") // Redeem emoji placeholder
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await FollowupAsync(embed: successEmbed);
        }
        else
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Gift Failed")
                .WithDescription(result.Message)
                .WithColor(Color.Red)
                .Build();

            await FollowupAsync(embed: errorEmbed, ephemeral: true);
        }
    }

    /// <summary>
    ///     Gifts a Pokemon to another user.
    /// </summary>
    /// <param name="user">The user to gift the Pokemon to.</param>
    /// <param name="pokemon">The position of the Pokemon to gift.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("pokemon", "Gift a Pokemon to another user")]
    [TradeLock]
    public async Task GiftPokemon(
        [Summary("user", "The user to receive the Pokemon")]
        IUser user,
        [Summary("pokemon", "The position number of the Pokemon you want to gift")]
        int pokemon)
    {
        if (pokemon <= 1)
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Cannot Gift That Pokemon")
                .WithDescription("You cannot give away your first Pokemon or use position 0!")
                .WithColor(Color.Red)
                .Build();

            await RespondAsync(embed: errorEmbed, ephemeral: true);
            return;
        }

        if (ctx.User.Id == user.Id)
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Cannot Gift Yourself")
                .WithDescription("You cannot give a Pokemon to yourself.")
                .WithColor(Color.Red)
                .Build();

            await RespondAsync(embed: errorEmbed, ephemeral: true);
            return;
        }

        // Show confirmation dialog
        var confirmEmbed = new EmbedBuilder()
            .WithTitle("🎁 Confirm Gift")
            .WithDescription($"Are you sure you want to give your Pokemon at position **{pokemon}** to {user.Mention}?")
            .WithColor(Color.Orange)
            .WithFooter("This action cannot be undone.");
        if (!await PromptUserConfirmAsync(confirmEmbed, ctx.User.Id))
        {
            var cancelEmbed = new EmbedBuilder()
                .WithTitle("❌ Gift Cancelled")
                .WithDescription("The gift has been cancelled.")
                .WithColor(Color.Red)
                .Build();

            await FollowupAsync(embed: cancelEmbed, ephemeral: true);
            return;
        }

        var result = await Service.GiftPokemonAsync(ctx.User.Id, user.Id, pokemon);

        if (result.Success)
        {
            var successEmbed = new EmbedBuilder()
                .WithTitle("🎉 Gift Successful!")
                .WithDescription($"{ctx.User.Mention} has gifted a **{result.Amount}** to {user.Mention}!")
                .WithColor(Color.Green)
                .WithThumbnailUrl(
                    "https://cdn.discordapp.com/emojis/1008748071584616569.png") // Pokemon gift emoji placeholder
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await FollowupAsync(embed: successEmbed);
        }
        else
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Gift Failed")
                .WithDescription(result.Message)
                .WithColor(Color.Red)
                .Build();

            await FollowupAsync(embed: errorEmbed, ephemeral: true);
        }
    }

    /// <summary>
    ///     Gifts tokens to another user.
    /// </summary>
    /// <param name="user">The user to gift tokens to.</param>
    /// <param name="type">The type of tokens to gift.</param>
    /// <param name="amount">The amount of tokens to gift.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("tokens", "Gift radiant tokens to another user")]
    [TradeLock]
    public async Task GiftTokens(
        [Summary("user", "The user to gift tokens to")]
        IUser user,
        [Summary("type", "The type of tokens to gift")]
        [Choice("Dark", "Dark")]
        [Choice("Bug", "Bug")]
        [Choice("Ground", "Ground")]
        [Choice("Fighting", "Fighting")]
        [Choice("Steel", "Steel")]
        [Choice("Electric", "Electric")]
        [Choice("Grass", "Grass")]
        [Choice("Fairy", "Fairy")]
        [Choice("Water", "Water")]
        [Choice("Rock", "Rock")]
        [Choice("Flying", "Flying")]
        [Choice("Psychic", "Psychic")]
        [Choice("Normal", "Normal")]
        [Choice("Dragon", "Dragon")]
        [Choice("Fire", "Fire")]
        [Choice("Ghost", "Ghost")]
        [Choice("Ice", "Ice")]
        [Choice("Poison", "Poison")]
        string type,
        [Summary("amount", "The amount of tokens to gift")]
        int amount)
    {
        if (amount <= 0)
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Invalid Amount")
                .WithDescription("You need to gift at least 1 token!")
                .WithColor(Color.Red)
                .Build();

            await RespondAsync(embed: errorEmbed, ephemeral: true);
            return;
        }

        if (ctx.User.Id == user.Id)
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Cannot Gift Yourself")
                .WithDescription("You cannot give yourself tokens.")
                .WithColor(Color.Red)
                .Build();

            await RespondAsync(embed: errorEmbed, ephemeral: true);
            return;
        }

        if (!TokenTypeExtensions.TryParse(type, out var tokenType))
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Invalid Token Type")
                .WithDescription($"Invalid token type: {type}. Please enter a valid type.")
                .WithColor(Color.Red)
                .Build();

            await RespondAsync(embed: errorEmbed, ephemeral: true);
            return;
        }

        // Show confirmation dialog
        var confirmEmbed = new EmbedBuilder()
            .WithTitle("🎁 Confirm Gift")
            .WithDescription(
                $"Are you sure you want to give **{amount}** {tokenType.GetEmoji()} **{tokenType.GetDisplayName()}** tokens to {user.Mention}?")
            .WithColor(Color.Orange)
            .WithFooter("This action cannot be undone.");
        if (!await PromptUserConfirmAsync(confirmEmbed, ctx.User.Id))
        {
            var cancelEmbed = new EmbedBuilder()
                .WithTitle("❌ Gift Cancelled")
                .WithDescription("The gift has been cancelled.")
                .WithColor(Color.Red)
                .Build();

            await FollowupAsync(embed: cancelEmbed, ephemeral: true);
            return;
        }

        var result = await Service.GiftTokensAsync(ctx.User.Id, user.Id, tokenType, amount);

        if (result.Success)
        {
            var successEmbed = new EmbedBuilder()
                .WithTitle("🎉 Gift Successful!")
                .WithDescription(
                    $"{ctx.User.Mention} has gifted **{amount}** {tokenType.GetEmoji()} **{tokenType.GetDisplayName()}** tokens to {user.Mention}!")
                .WithColor(Color.Green)
                .WithThumbnailUrl(tokenType.GetEmoji()) // Use the token emoji as thumbnail if possible
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await FollowupAsync(embed: successEmbed);
        }
        else
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Gift Failed")
                .WithDescription(result.Message)
                .WithColor(Color.Red)
                .Build();

            await FollowupAsync(embed: errorEmbed, ephemeral: true);
        }
    }

    /// <summary>
    ///     Shows help information about gifting.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("help", "Get help with gifting")]
    public async Task GiftHelp()
    {
        var embed = new EmbedBuilder()
            .WithTitle("🎁 Gifting Help")
            .WithDescription("Learn how to gift items to other users!")
            .WithColor(Color.Gold)
            .AddField("**Gift Credits**",
                "Use `/gift credits @user amount` to give Coins to another user.\n" +
                "Example: `/gift credits @friend 10000`")
            .AddField("**Gift Redeems**",
                "Use `/gift redeems @user amount` to give redeems to another user.\n" +
                "Example: `/gift redeems @friend 3`")
            .AddField("**Gift Pokemon**",
                "Use `/gift pokemon @user position` to give a Pokemon to another user.\n" +
                "Example: `/gift pokemon @friend 5`\n" +
                "⚠️ You cannot gift your #1 Pokemon, eggs, or favorited Pokemon")
            .AddField("**Gift Tokens**",
                "Use `/gift tokens @user type amount` to give radiant tokens.\n" +
                "Example: `/gift tokens @friend Fire 5`\n" +
                "Available types: Fire, Water, Grass, Electric, and 14 others")
            .AddField("**Important Notes**",
                "• All gifts require confirmation before sending\n" +
                "• Gifts cannot be undone once sent\n" +
                "• Both users must have started (`/start`) to gift\n" +
                "• Trade-locked users cannot send or receive gifts\n" +
                "• All gifts are logged for security")
            .WithFooter("Use `/trade` to exchange items with another user!")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await RespondAsync(embed: embed, ephemeral: true);
    }
}