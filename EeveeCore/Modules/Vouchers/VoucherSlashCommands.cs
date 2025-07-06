using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Vouchers.Models;
using EeveeCore.Modules.Vouchers.Services;
using EeveeCore.Modules.Vouchers.Components;
using Serilog;

namespace EeveeCore.Modules.Vouchers;

/// <summary>
///     Provides Discord slash commands for voucher request functionality.
///     Allows users to submit voucher requests for custom Pokemon artwork.
/// </summary>
/// <param name="voucherService">Service for handling voucher request operations.</param>
[Group("voucher", "Voucher request system for custom Pokemon artwork")]
public class VoucherSlashCommands(VoucherService voucherService) 
    : EeveeCoreSlashModuleBase<VoucherService>
{
    /// <summary>
    ///     Starts a new voucher request form for custom Pokemon artwork.
    ///     Users can specify what Pokemon they want, appearance details, payment method, and artist preferences.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("request", "Submit a voucher request for custom Pokemon artwork")]
    public async Task VoucherRequestCommand()
    {
        try
        {
            await DeferAsync(ephemeral: true);

            // Check if user has vouchers available
            var hasVouchers = await voucherService.HasAvailableVouchersAsync(ctx.User.Id);
            if (!hasVouchers)
            {
                var noVouchersEmbed = new EmbedBuilder()
                    .WithTitle("❌ No Vouchers Available")
                    .WithDescription("You don't have any vouchers available to spend.\n\n" +
                                   "Vouchers can be earned through various activities in the bot.")
                    .WithColor(Color.Red)
                    .Build();

                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = noVouchersEmbed;
                });
                return;
            }

            // Check if user can create more requests
            var canCreateMore = await voucherService.CanUserCreateMoreRequestsAsync(ctx.User.Id);
            if (!canCreateMore)
            {
                var existingRequests = await voucherService.GetUserVoucherRequestsAsync(ctx.User.Id);
                
                var limitEmbed = new EmbedBuilder()
                    .WithTitle("⚠️ Request Limit Reached")
                    .WithDescription($"You have reached the maximum number of active voucher requests ({existingRequests.Count}).\n\n" +
                                   "Please wait for your current requests to be processed before submitting new ones.")
                    .WithColor(Color.Orange);

                // Show existing requests status
                if (existingRequests.Any())
                {
                    var statusText = string.Join("\n", existingRequests.Select(r => 
                        $"• Request #{r.RequestId}: {VoucherService.FormatStatusList(r.Status)}"));
                    
                    limitEmbed.AddField("Your Active Requests", statusText, false);
                }

                var components = new ComponentBuilder()
                    .WithButton("Create New Request", "voucher:new_request", ButtonStyle.Primary, 
                        new Emoji("📝"), disabled: true)
                    .Build();

                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = limitEmbed.Build();
                    x.Components = components;
                });
                return;
            }

            // Create new form
            var formId = Guid.NewGuid().ToString();
            var formData = new VoucherRequestFormData();
            VoucherFormManager.StoreFormData(formId, formData);

            var embed = await CreateFormEmbedAsync(formData);
            var formComponents = CreateFormComponents(formId);

            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "";
                x.Embed = embed;
                x.Components = formComponents;
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error creating voucher request for user {UserId}", ctx.User.Id);
            await ErrorAsync("An error occurred while setting up the voucher request form.");
        }
    }

    /// <summary>
    ///     Shows the status of all voucher requests for the current user.
    ///     Displays active requests and their current processing status.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("status", "View the status of your voucher requests")]
    public async Task VoucherStatusCommand()
    {
        try
        {
            await DeferAsync(ephemeral: true);

            var requests = await voucherService.GetUserVoucherRequestsAsync(ctx.User.Id);
            
            if (!requests.Any())
            {
                var noRequestsEmbed = new EmbedBuilder()
                    .WithTitle("📋 Your Voucher Requests")
                    .WithDescription("You haven't submitted any voucher requests yet.\n\n" +
                                   "Use `/voucher request` to create your first request!")
                    .WithColor(Color.Blue)
                    .Build();

                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = noRequestsEmbed;
                });
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("📋 Your Voucher Requests")
                .WithDescription($"You have {requests.Count} voucher request(s):")
                .WithColor(Color.Blue);

            foreach (var request in requests.Take(10)) // Limit to 10 to avoid embed size issues
            {
                var statusColor = VoucherService.GetStatusColor(request.Status);
                var statusText = VoucherService.FormatStatusList(request.Status);
                
                embed.AddField($"Request #{request.RequestId}", 
                    $"**Status:** {statusText}\n" +
                    $"**Pokemon:** {request.Pokemon ?? "Not specified"}\n" +
                    $"**Created:** <t:{request.CreatedAt.ToUnixTimeSeconds()}:R>", 
                    false);
            }

            if (requests.Count > 10)
            {
                embed.WithFooter($"... and {requests.Count - 10} more requests");
            }

            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embed.Build();
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error retrieving voucher status for user {UserId}", ctx.User.Id);
            await ErrorAsync("An error occurred while retrieving your voucher request status.");
        }
    }

    /// <summary>
    ///     Shows information about the voucher request system.
    ///     Explains how to use vouchers and what they can be used for.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("info", "Learn about the voucher request system")]
    public async Task VoucherInfoCommand()
    {
        try
        {
            var embed = new EmbedBuilder()
                .WithTitle("🎫 Voucher Request System")
                .WithDescription("Welcome to the voucher request system! Here's everything you need to know:")
                .WithColor(Color.Purple);

            embed.AddField("🎯 **What are Vouchers?**",
                "Vouchers are special currency that can be redeemed for custom Pokemon artwork.\n" +
                "They're earned through various activities and events in the bot.", false);

            embed.AddField("🎨 **How to Use Vouchers**",
                "1. Use `/voucher request` to start a new request\n" +
                "2. Fill out the form with your Pokemon and appearance details\n" +
                "3. Specify your payment method and artist preferences\n" +
                "4. Submit your request and wait for processing", false);

            embed.AddField("📋 **Request Process**",
                "• Your request will be reviewed by staff\n" +
                "• Artists will be assigned based on availability\n" +
                "• You'll be notified of status updates\n" +
                "• Completed artwork will be delivered to you", false);

            embed.AddField("⚠️ **Important Notes**",
                "• You can have multiple active requests\n" +
                "• Requests cannot be cancelled once submitted\n" +
                "• Processing time varies based on complexity\n" +
                "• Follow community guidelines for requests", false);

            embed.AddField("🔧 **Commands**",
                "`/voucher request` - Submit a new request\n" +
                "`/voucher status` - View your request status\n" +
                "`/voucher info` - Show this information", false);

            embed.WithFooter("Happy requesting! 🎨");

            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error showing voucher info for user {UserId}", ctx.User.Id);
            await ErrorAsync("An error occurred while retrieving voucher information.");
        }
    }

    /// <summary>
    ///     Creates the form embed showing current completion status.
    /// </summary>
    /// <param name="formData">The current form data.</param>
    /// <returns>A task that returns the created embed.</returns>
    private static async Task<Embed> CreateFormEmbedAsync(VoucherRequestFormData formData)
    {
        var embed = new EmbedBuilder()
            .WithTitle("🎫 Voucher Request Form")
            .WithDescription($"Please fill out all fields to submit your request.\n\n**Completion: {formData.CompletionPercentage}%**")
            .WithColor(formData.IsComplete ? Color.Green : Color.Orange);

        embed.AddField("🎯 Pokemon", 
            string.IsNullOrWhiteSpace(formData.Pokemon) ? "❌ Not filled" : $"✅ {formData.Pokemon}", 
            true);

        embed.AddField("🎨 Appearance", 
            string.IsNullOrWhiteSpace(formData.Appearance) ? "❌ Not filled" : "✅ Filled", 
            true);

        embed.AddField("💰 Payment", 
            string.IsNullOrWhiteSpace(formData.PaymentMethod) ? "❌ Not filled" : $"✅ {formData.PaymentMethod}", 
            true);

        embed.AddField("👨‍🎨 Artist", 
            string.IsNullOrWhiteSpace(formData.Artist.ToString()) ? "❌ Not specified" : $"✅ {formData.Artist}", 
            true);

        return embed.Build();
    }

    /// <summary>
    ///     Creates the form interaction components.
    /// </summary>
    /// <param name="formId">The unique form identifier.</param>
    /// <returns>The message components for the form.</returns>
    private static MessageComponent CreateFormComponents(string formId)
    {
        var builder = new ComponentBuilder();

        // First row - main form fields
        builder.WithButton("🎯 Pokemon", $"voucher:pokemon:{formId}", ButtonStyle.Primary)
               .WithButton("🎨 Appearance", $"voucher:appearance:{formId}", ButtonStyle.Primary)
               .WithButton("💰 Payment", $"voucher:payment:{formId}", ButtonStyle.Primary)
               .WithButton("👨‍🎨 Artist", $"voucher:artist:{formId}", ButtonStyle.Secondary);

        // Second row - submit button
        builder.WithButton("📤 Submit Request", $"voucher:submit:{formId}", ButtonStyle.Success, row: 1);

        return builder.Build();
    }
}