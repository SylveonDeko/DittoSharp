using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Vouchers.Common;
using EeveeCore.Modules.Vouchers.Models;
using EeveeCore.Modules.Vouchers.Services;
using Serilog;

namespace EeveeCore.Modules.Vouchers.Components;

/// <summary>
///     Handles interaction components for the voucher request system.
///     Processes button clicks and modal submissions for voucher form interactions.
/// </summary>
/// <param name="voucherService">The service that handles voucher request operations.</param>
public class VoucherInteractionModule(VoucherService voucherService) 
    : EeveeCoreSlashModuleBase<VoucherService>
{
    /// <summary>
    ///     Handles the Pokemon button interaction for voucher requests.
    ///     Shows a modal for the user to input what Pokemon they want.
    /// </summary>
    /// <param name="formId">The unique form identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("voucher:pokemon:*")]
    public async Task HandlePokemonButton(string formId)
    {
        try
        {
            await ctx.Interaction.RespondWithModalAsync<VoucherPokemonModal>($"voucher_pokemon_modal:{formId}");
        }
        catch (Exception e)
        {
            Log.Error(e, "Error handling Pokemon button for form {FormId}", formId);
            await ctx.Interaction.RespondAsync("An error occurred. Please try again.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Handles the appearance button interaction for voucher requests.
    ///     Shows a modal for the user to describe how they want the Pokemon to look.
    /// </summary>
    /// <param name="formId">The unique form identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("voucher:appearance:*")]
    public async Task HandleAppearanceButton(string formId)
    {
        try
        {
            await ctx.Interaction.RespondWithModalAsync<VoucherAppearanceModal>($"voucher_appearance_modal:{formId}");
        }
        catch (Exception e)
        {
            Log.Error(e, "Error handling appearance button for form {FormId}", formId);
            await ctx.Interaction.RespondAsync("An error occurred. Please try again.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Handles the payment button interaction for voucher requests.
    ///     Shows a modal for the user to specify their payment method.
    /// </summary>
    /// <param name="formId">The unique form identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("voucher:payment:*")]
    public async Task HandlePaymentButton(string formId)
    {
        try
        {
            await ctx.Interaction.RespondWithModalAsync<VoucherPaymentModal>($"voucher_payment_modal:{formId}");
        }
        catch (Exception e)
        {
            Log.Error(e, "Error handling payment button for form {FormId}", formId);
            await ctx.Interaction.RespondAsync("An error occurred. Please try again.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Handles the artist button interaction for voucher requests.
    ///     Shows a modal for the user to specify any artist preferences.
    /// </summary>
    /// <param name="formId">The unique form identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("voucher:artist:*")]
    public async Task HandleArtistButton(string formId)
    {
        try
        {
            await ctx.Interaction.RespondWithModalAsync<VoucherArtistModal>($"voucher_artist_modal:{formId}");
        }
        catch (Exception e)
        {
            Log.Error(e, "Error handling artist button for form {FormId}", formId);
            await ctx.Interaction.RespondAsync("An error occurred. Please try again.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Handles the submit button interaction for voucher requests.
    ///     Creates a forum thread with the completed request.
    /// </summary>
    /// <param name="formId">The unique form identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("voucher:submit:*")]
    public async Task HandleSubmitButton(string formId)
    {
        try
        {
            await DeferAsync();

            var formData = VoucherFormManager.GetFormData(formId);
            if (formData == null)
            {
                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Form session not found or has expired. Please start over.";
                    x.Embed = null;
                    x.Components = null;
                });
                return;
            }

            if (!formData.IsComplete)
            {
                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Please fill out all required fields before submitting.";
                });
                return;
            }

            // Get the target guild and forum channel
            var guild = await ctx.Client.GetGuildAsync(VoucherConstants.VoucherGuildId);
            if (guild == null)
            {
                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Target guild not found. Please contact staff.";
                    x.Embed = null;
                    x.Components = null;
                });
                return;
            }

            var forumChannel = await guild.GetForumChannelAsync(VoucherConstants.VoucherForumChannelId);
            if (forumChannel == null)
            {
                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Forum channel not found. Please contact staff.";
                    x.Embed = null;
                    x.Components = null;
                });
                return;
            }

            // Create the request embed
            var requestEmbed = new EmbedBuilder()
                .WithTitle("New Voucher Request")
                .WithDescription($"Requested by {ctx.User.Mention}")
                .WithColor(Color.Blue)
                .AddField("Pokemon", formData.Pokemon, true)
                .AddField("Appearance", formData.Appearance.Length > 1024 ? formData.Appearance[..1021] + "..." : formData.Appearance, false)
                .AddField("Payment Method", formData.PaymentMethod, true)
                .AddField("Artist Preference", string.IsNullOrEmpty(formData.Artist.ToString()) ? "No preference" : formData.Artist, true)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            // Create status management view
            var statusView = new StatusManagementView(ctx.User.Id);

            // Create the forum thread
            var thread = await forumChannel.CreatePostAsync(
                $"Voucher Request - {ctx.User.Username}",
                embed: requestEmbed,
                components: statusView.Build());

            // Save the request to database
            await voucherService.CreateVoucherRequestAsync(
                ctx.User.Id,
                formData,
                thread.Id,
                thread.Id);

            // Clean up form data
            VoucherFormManager.RemoveFormData(formId);

            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "✅ Your voucher request has been submitted successfully!";
                x.Embed = null;
                x.Components = null;
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error submitting voucher request for form {FormId}", formId);
            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "An error occurred while submitting your request. Please try again.";
                x.Embed = null;
                x.Components = null;
            });
        }
    }

    /// <summary>
    ///     Handles the new request button for users with existing requests.
    ///     Starts a new voucher request form.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("voucher:new_request")]
    public async Task HandleNewRequestButton()
    {
        try
        {
            await DeferAsync();

            var formId = Guid.NewGuid().ToString();
            var formData = new VoucherRequestFormData();
            VoucherFormManager.StoreFormData(formId, formData);

            var embed = await CreateFormEmbedAsync(formData);
            var components = CreateFormComponents(formId);

            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "";
                x.Embed = embed;
                x.Components = components;
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error creating new voucher request for user {UserId}", ctx.User.Id);
            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "An error occurred while creating a new request form.";
                x.Embed = null;
                x.Components = null;
            });
        }
    }

    /// <summary>
    ///     Handles modal submissions for Pokemon selection.
    /// </summary>
    /// <param name="formId">The unique form identifier.</param>
    /// <param name="modal">The submitted modal data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ModalInteraction("voucher_pokemon_modal:*")]
    public async Task HandlePokemonModal(string formId, VoucherPokemonModal modal)
    {
        await HandleModalSubmissionAsync(formId, form => form.Pokemon = modal.Pokemon);
    }

    /// <summary>
    ///     Handles modal submissions for appearance description.
    /// </summary>
    /// <param name="formId">The unique form identifier.</param>
    /// <param name="modal">The submitted modal data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ModalInteraction("voucher_appearance_modal:*")]
    public async Task HandleAppearanceModal(string formId, VoucherAppearanceModal modal)
    {
        await HandleModalSubmissionAsync(formId, form => form.Appearance = modal.Appearance);
    }

    /// <summary>
    ///     Handles modal submissions for payment method.
    /// </summary>
    /// <param name="formId">The unique form identifier.</param>
    /// <param name="modal">The submitted modal data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ModalInteraction("voucher_payment_modal:*")]
    public async Task HandlePaymentModal(string formId, VoucherPaymentModal modal)
    {
        await HandleModalSubmissionAsync(formId, form => form.PaymentMethod = modal.PaymentMethod);
    }

    /// <summary>
    ///     Handles modal submissions for artist preference.
    /// </summary>
    /// <param name="formId">The unique form identifier.</param>
    /// <param name="modal">The submitted modal data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ModalInteraction("voucher_artist_modal:*")]
    public async Task HandleArtistModal(string formId, VoucherArtistModal modal)
    {
        await HandleModalSubmissionAsync(formId, form => form.Artist = modal.Artist);;
    }

    /// <summary>
    ///     Handles status update selections for voucher requests.
    /// </summary>
    /// <param name="values">The selected status values.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("voucher_status_select")]
    public async Task HandleStatusSelect(string[] values)
    {
        try
        {
            await DeferAsync(ephemeral: true);

            // Check if user has permission to manage voucher requests
            if (!VoucherConstants.AllowedManagerIds.Contains(ctx.User.Id))
            {
                await ctx.Interaction.FollowupAsync("You don't have permission to manage voucher requests.", ephemeral: true);
                return;
            }

            var messageId = ctx.Interaction.Id;
            var success = await voucherService.UpdateVoucherRequestStatusAsync(messageId, values.ToList());

            if (success)
            {
                await ctx.Interaction.FollowupAsync($"Status updated to: {string.Join(", ", values)}", ephemeral: true);
            }
            else
            {
                await ctx.Interaction.FollowupAsync("Failed to update status. Request may not exist.", ephemeral: true);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error updating voucher request status");
            await ctx.Interaction.FollowupAsync("An error occurred while updating the status.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Common handler for modal submissions that update form data.
    /// </summary>
    /// <param name="formId">The unique form identifier.</param>
    /// <param name="updateAction">The action to update the form data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleModalSubmissionAsync(string formId, Action<VoucherRequestFormData> updateAction)
    {
        try
        {
            await DeferAsync();

            var formData = VoucherFormManager.GetFormData(formId);
            if (formData == null)
            {
                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Form session not found or has expired. Please start over.";
                    x.Embed = null;
                    x.Components = null;
                });
                return;
            }

            updateAction(formData);
            VoucherFormManager.StoreFormData(formId, formData);

            var embed = await CreateFormEmbedAsync(formData);
            var components = CreateFormComponents(formId);

            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "";
                x.Embed = embed;
                x.Components = components;
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error handling modal submission for form {FormId}", formId);
            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "An error occurred while processing your input.";
                x.Embed = null;
                x.Components = null;
            });
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
            .WithTitle("Voucher Request Form")
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

        await Task.CompletedTask;
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

/// <summary>
///     View for managing voucher request statuses by administrators.
/// </summary>
/// <param name="userId">The user ID who submitted the request.</param>
public class StatusManagementView(ulong userId)
{
    /// <summary>
    ///     Builds the message components for status management.
    /// </summary>
    /// <returns>The message components for status management.</returns>
    public MessageComponent Build()
    {
        var options = VoucherConstants.StatusOptions.Select(status => 
            new SelectMenuOptionBuilder(status, status)).ToList();

        var selectMenu = new SelectMenuBuilder()
            .WithCustomId("voucher_status_select")
            .WithPlaceholder("Update request status...")
            .WithMinValues(1)
            .WithMaxValues(VoucherConstants.StatusOptions.Length)
            .WithOptions(options);

        return new ComponentBuilder()
            .WithSelectMenu(selectMenu)
            .Build();
    }
}

/// <summary>
///     Simple in-memory form data manager.
///     In production, you would want to use Redis or a proper cache.
/// </summary>
public static class VoucherFormManager
{
    private static readonly Dictionary<string, VoucherRequestFormData> FormData = new();
    private static readonly object Lock = new();

    /// <summary>
    ///     Stores form data in memory.
    /// </summary>
    /// <param name="formId">The form identifier.</param>
    /// <param name="data">The form data to store.</param>
    public static void StoreFormData(string formId, VoucherRequestFormData data)
    {
        lock (Lock)
        {
            FormData[formId] = data;
        }
    }

    /// <summary>
    ///     Retrieves form data from memory.
    /// </summary>
    /// <param name="formId">The form identifier.</param>
    /// <returns>The form data, or null if not found.</returns>
    public static VoucherRequestFormData? GetFormData(string formId)
    {
        lock (Lock)
        {
            return FormData.TryGetValue(formId, out var data) ? data : null;
        }
    }

    /// <summary>
    ///     Removes form data from memory.
    /// </summary>
    /// <param name="formId">The form identifier.</param>
    public static void RemoveFormData(string formId)
    {
        lock (Lock)
        {
            FormData.Remove(formId);
        }
    }

    /// <summary>
    ///     Cleans up expired form data.
    /// </summary>
    /// <param name="maxAge">The maximum age before form data is considered expired.</param>
    public static void CleanupExpiredForms(TimeSpan maxAge)
    {
        lock (Lock)
        {
            // In a real implementation, you'd track creation times
            // For now, we'll just clear all forms older than the timeout
            FormData.Clear();
        }
    }
}