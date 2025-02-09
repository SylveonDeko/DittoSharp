using System.Text;
using Discord.Interactions;
using Ditto.Common.ModuleBases;
using Ditto.Modules.Pokemon.Services;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Humanizer;
using Serilog;

namespace Ditto.Modules.Pokemon;

[Group("pokemon", "Pokemon related commands")]
public class PokemonSlashCommands : DittoSlashModuleBase<PokemonService>
{
    private readonly InteractiveService _interactivity;
    private readonly Random _random = new();

    private readonly string[] _footers =
    [
        "Use /donate for exclusive rewards - Thank you everyone for your continued support!",
        "Upvote DittoBOT with the /vote command and get energy, credits, redeems and more!",
        "The latest updates can be viewed with the /updates command!",
        "Join our official server! discord.gg/ditto <3.",
        "Take a look at one of our partners bots! Mewdeko - discord.gg/mewdeko, we think its the best all-purpose bot around!",
        "We are always looking for new help! Art Team, Staff Team, and Dev team-ask in the official server!"
    ];

    public PokemonSlashCommands(InteractiveService interactivity)
    {
        _interactivity = interactivity;
    }

    [SlashCommand("select", "Select a pokemon by ID number")]
    [RequireContext(ContextType.Guild)]
    public async Task SelectPokemon(int pokeId)
    {
        await DeferAsync();
        var result = await Service.SelectPokemon(ctx.User.Id, pokeId);
        if (!result.Success)
        {
            await ctx.Interaction.SendErrorFollowupAsync(result.Message);
            return;
        }

        await ctx.Interaction.SendConfirmFollowupAsync(result.Message);
    }

    [SlashCommand("info", "Get information about a pokemon")]
    [RequireContext(ContextType.Guild)]
    public async Task Info(string poke = null, PokemonVariantType? variant = null)
    {
        try
        {
            Database.Models.PostgreSQL.Pokemon.Pokemon ownedPokemon = null;

            // Case 1: No parameter - get selected Pokemon
            if (string.IsNullOrWhiteSpace(poke))
            {
                ownedPokemon = await Service.GetSelectedPokemon(ctx.User.Id);
                if (ownedPokemon == null)
                {
                    await ctx.Interaction.SendErrorAsync(
                        "You do not have a pokemon selected. Use `/select` to select one!");
                    return;
                }

                await DisplayOwnedPokemonInfo(ownedPokemon);
                return;
            }

            // Case 2: "new", "newest", "latest" - get newest Pokemon
            if (poke.ToLower() is "newest" or "latest" or "atest" or "ewest" or "new")
            {
                ownedPokemon = await Service.GetNewestPokemon(ctx.User.Id);
                if (ownedPokemon == null)
                {
                    await ctx.Interaction.SendErrorAsync("You have not started!\nStart with `/start` first.");
                    return;
                }

                await DisplayOwnedPokemonInfo(ownedPokemon);
                return;
            }

            // Case 3: Numeric input - get Pokemon by number from user's list
            if (int.TryParse(poke, out var pokeNumber))
            {
                if (pokeNumber < 1)
                {
                    await ctx.Interaction.SendErrorAsync("That is not a valid pokemon number!");
                    return;
                }

                if (pokeNumber > 4000000000)
                {
                    await ctx.Interaction.SendErrorAsync("You probably don't have that many pokemon...");
                    return;
                }

                ownedPokemon = await Service.GetPokemonByNumber(ctx.User.Id, pokeNumber);
                if (ownedPokemon == null)
                {
                    await ctx.Interaction.SendErrorAsync(
                        "You do not have that many pokemon. Go catch some more first!");
                    return;
                }

                await DisplayOwnedPokemonInfo(ownedPokemon);
                return;
            }

            // Case 4: Pokemon name with potential variants
            var pokemonName = poke.ToLower().Replace("alolan", "alola").Split(' ');
            var shiny = false;
            var radiant = false;
            string skin = null;

            // Handle variant parameter
            if (variant.HasValue)
                switch (variant.Value)
                {
                    case PokemonVariantType.Shiny:
                        shiny = true;
                        break;
                    case PokemonVariantType.Radiant:
                        radiant = true;
                        break;
                    case PokemonVariantType.Shadow:
                        skin = "shadow";
                        break;
                }

            // Check name for variants
            if (pokemonName.Contains("shiny"))
            {
                shiny = true;
                pokemonName = pokemonName.Where(v => v != "shiny").ToArray();
            }
            else if (pokemonName.Contains("radiant"))
            {
                radiant = true;
                pokemonName = pokemonName.Where(v => v != "radiant").ToArray();
            }
            else if (pokemonName.Contains("shadow") && Array.IndexOf(pokemonName, "shadow") == 0)
            {
                skin = "shadow";
                pokemonName = pokemonName.Where(v => v != "shadow").ToArray();
            }

            var finalName = string.Join("-", pokemonName);
            var val = finalName.ToTitleCase();
            var pokemon = await Service.GetPokemonInfo(finalName);
            var (form, imageUrl) = await Service.GetPokemonFormInfo(pokemon.Name,
                pokemon.Name.ToLower().Contains("shiny"));

            // Get forms
            var forms = await Service.GetPokemonForms(val);
            if (string.IsNullOrEmpty(forms)) forms = "None";

            // Get Pokemon info
            var pokemonInfo = await Service.GetPokemonInfo(val);
            if (pokemonInfo == null)
            {
                await ctx.Interaction.SendErrorAsync(
                    "<:error:1009448089930694766> Oops! It seems the pokemon you tried to view with `/i` is currently having some issues on our side!\nPlease try again later!");
                return;
            }

            // Build the embed
            var emoji = GetVariantEmoji(shiny, radiant, skin);
            var embed = new EmbedBuilder()
                .WithTitle($"{emoji}{val}")
                .WithColor(new Color(_random.Next(256), _random.Next(256), _random.Next(256)))
                .WithFooter(_footers[_random.Next(_footers.Length)]);

            var infoField = new StringBuilder();
            infoField.AppendLine($"**Abilities**: `{string.Join(", ", pokemonInfo.Abilities).ToTitleCase()}`");
            infoField.AppendLine($"**Types**: {string.Join(", ", pokemonInfo.Types.Select(GetTypeEmote))}");
            infoField.AppendLine($"**Egg Groups**: {string.Join(", ", pokemonInfo.EggGroups.Select(GetEggEmote))}");
            infoField.AppendLine($"**Weight**: `{pokemonInfo.Weight} kg`");

            var statsStr = new StringBuilder();
            statsStr.AppendLine($"{PokemonDisplayConstants.HP_DISPLAY} `{pokemonInfo.Stats.Hp}`");
            statsStr.AppendLine($"{PokemonDisplayConstants.ATK_DISPLAY} `{pokemonInfo.Stats.Attack}`");
            statsStr.AppendLine($"{PokemonDisplayConstants.DEF_DISPLAY} `{pokemonInfo.Stats.Defense}`");
            statsStr.AppendLine($"{PokemonDisplayConstants.SPATK_DISPLAY} `{pokemonInfo.Stats.SpecialAttack}`");
            statsStr.AppendLine($"{PokemonDisplayConstants.SPDEF_DISPLAY} `{pokemonInfo.Stats.SpecialDefense}`");
            statsStr.AppendLine($"{PokemonDisplayConstants.SPE_DISPLAY} `{pokemonInfo.Stats.Speed}`");

            infoField.AppendLine("\n**Stats**");
            infoField.Append(statsStr);

            embed.AddField("Pokemon Information", infoField.ToString());
            if (!string.IsNullOrEmpty(imageUrl)) embed.WithImageUrl(imageUrl);

            // Create and add the More Information button
            var components = new ComponentBuilder()
                .WithButton("More Information", $"pokeinfo:more,{pokemonInfo.Id}", ButtonStyle.Primary, new Emoji("ℹ️"))
                .Build();

            await ctx.Interaction.RespondAsync(embed: embed.Build(), components: components);
        }
        catch (Exception ex)
        {
            await ctx.Interaction.SendErrorAsync("That Pokemon does not exist!");
            Log.Error(ex, "Error in info command");
        }
    }

    [SlashCommand("pokedex", "View your Pokedex progress")]
    public async Task Pokedex(string variant = null)
    {
        await DeferAsync();
        if (string.IsNullOrEmpty(variant))
        {
            await ShowNationalPokedex();
            return;
        }

        switch (variant.ToLower())
        {
            case "national":
                await ShowNationalPokedex();
                break;
            case "unowned":
                await ShowUnownedPokedex();
                break;
            case "shadow":
            case "shiny":
            case "radiant":
            case "skin":
                await ShowSpecialPokedex(variant);
                break;
            default:
                await ctx.Interaction.SendErrorFollowupAsync(
                    "Invalid variant selected. Available options: national, unowned, shadow, shiny, radiant, skin");
                break;
        }
    }

    private async Task ShowNationalPokedex()
    {
        try
        {
            var pokemonList = await Service.GetAllPokemon();
            var ownedPokemon = await Service.GetUserPokemons(ctx.User.Id);

            var pages = new List<PageBuilder>();
            var currentPage = new StringBuilder();
            var pokemonCount = 0;

            foreach (var pokemon in pokemonList)
            {
                var isOwned = ownedPokemon.Any(p =>
                    p.PokemonName.Equals(pokemon.Identifier, StringComparison.OrdinalIgnoreCase));
                var status = isOwned ? "<:save:1337858300037042330>" : "<:delete:1337858391720202292>";
                currentPage.AppendLine($"**{pokemon.Identifier.Titleize()}** - {status}");
                pokemonCount++;

                if (pokemonCount % 20 == 0)
                {
                    pages.Add(new PageBuilder()
                        .WithColor(Color.Blue)
                        .WithDescription(currentPage.ToString())
                        .WithTitle(
                            $"National Pokedex ({ownedPokemon.DistinctBy(x => x.PokemonName).Count()}/{pokemonList.Count})"));
                    currentPage.Clear();
                }
            }

            if (currentPage.Length > 0)
                pages.Add(new PageBuilder()
                    .WithColor(Color.Blue)
                    .WithDescription(currentPage.ToString())
                    .WithTitle(
                        $"National Pokedex ({ownedPokemon.DistinctBy(x => x.PokemonName).Count()}/{pokemonList.Count})"));

            var paginator = new StaticPaginatorBuilder()
                .AddUser(ctx.User)
                .WithPages(pages)
                .WithDefaultEmotes()
                .WithFooter(PaginatorFooter.PageNumber)
                .Build();

            await _interactivity.SendPaginatorAsync(paginator, ctx.Interaction, TimeSpan.FromMinutes(10),
                InteractionResponseType.DeferredUpdateMessage);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task ShowSpecialPokedex(string variant)
    {
        var pokemonList = await Service.GetAllPokemon();
        var userPokemon = await Service.GetUserPokemons(ctx.User.Id);

        var ownedPokemon = variant switch
        {
            "shiny" => userPokemon.Where(p => p.Shiny == true),
            "radiant" => userPokemon.Where(p => p.Radiant == true),
            "shadow" => userPokemon.Where(p => p.Skin == "shadow"),
            "skin" => userPokemon.Where(p => !string.IsNullOrEmpty(p.Skin)),
            _ => userPokemon
        };

        var pages = new List<PageBuilder>();
        var currentPage = new StringBuilder();
        var pokemonCount = 0;

        var variantIcon = variant switch
        {
            "shiny" => "<:starrr:1175872035927375953>",
            "radiant" => "<a:newradhmm:1061418796021194883>",
            "shadow" => "<:shadowicon4:1077328251556470925>",
            "skin" => "<:skin23:1012754684576014416>",
            _ => "✨"
        };

        foreach (var pokemon in pokemonList)
        {
            var isOwned = ownedPokemon.Any(p =>
                p.PokemonName.Equals(pokemon.Identifier, StringComparison.OrdinalIgnoreCase));
            var status = isOwned ? variantIcon : "<:delete:1051241645447848009>";
            currentPage.AppendLine($"**{pokemon.Identifier.Titleize()}** - {status}");
            pokemonCount++;

            if (pokemonCount % 20 == 0)
            {
                pages.Add(new PageBuilder()
                    .WithColor(Color.Blue)
                    .WithDescription(currentPage.ToString())
                    .WithTitle($"{variant.Titleize()} Pokedex ({ownedPokemon.Count()}/{pokemonList.Count})"));
                currentPage.Clear();
            }
        }

        if (currentPage.Length > 0)
            pages.Add(new PageBuilder()
                .WithColor(Color.Blue)
                .WithDescription(currentPage.ToString())
                .WithTitle($"{variant.Titleize()} Pokedex ({ownedPokemon.Count()}/{pokemonList.Count})"));

        var paginator = new StaticPaginatorBuilder()
            .AddUser(ctx.User)
            .WithPages(pages)
            .WithDefaultEmotes()
            .WithFooter(PaginatorFooter.PageNumber)
            .Build();

        await _interactivity.SendPaginatorAsync(paginator, ctx.Interaction, TimeSpan.FromMinutes(10));
    }

    [SlashCommand("release", "Release a pokemon permanently")]
    public async Task Release(int pokemonNumber)
    {
        try
        {
            var pokemon = await Service.GetPokemonByNumber(ctx.User.Id, pokemonNumber);
            if (pokemon == null)
            {
                await ctx.Interaction.SendErrorAsync("That pokemon does not exist!");
                return;
            }

            var ivPercentage = (pokemon.HpIv + pokemon.AttackIv + pokemon.DefenseIv +
                                pokemon.SpecialAttackIv + pokemon.SpecialDefenseIv + pokemon.SpeedIv) / 186.0 * 100;

            var confirmEmbed = new EmbedBuilder()
                .WithTitle("Confirm Release")
                .WithDescription($"Are you sure you want to release your {pokemon.PokemonName} ({ivPercentage:F2}%)?")
                .WithColor(Color.Red)
                .WithFooter("This action cannot be undone!");

            var result = await PromptUserConfirmAsync(confirmEmbed, ctx.User.Id);

            if (!result)
            {
                await ctx.Interaction.SendErrorAsync("Release cancelled.");
                return;
            }

            await Service.RemoveUserPokemon(ctx.User.Id, pokemon.Id, true);
            await ctx.Interaction.SendConfirmAsync($"You have successfully released your {pokemon.PokemonName}!");
        }
        catch (Exception ex)
        {
            await ctx.Interaction.SendErrorAsync(ex.Message);
        }
    }

    [SlashCommand("sacrifice", "Sacrifice a pokemon to fill your soul-gauge")]
    public async Task Sacrifice(int pokemonNumber)
    {
        try
        {
            var pokemon = await Service.GetPokemonByNumber(ctx.User.Id, pokemonNumber);
            if (pokemon == null)
            {
                await ctx.Interaction.SendErrorAsync("That pokemon does not exist!");
                return;
            }

            var ivPercentage = (pokemon.HpIv + pokemon.AttackIv + pokemon.DefenseIv +
                                pokemon.SpecialAttackIv + pokemon.SpecialDefenseIv + pokemon.SpeedIv) / 186.0 * 100;

            var soulIncrement = ivPercentage switch
            {
                <= 49.99 => _random.NextDouble() * (0.03 - 0.01) + 0.01,
                <= 59.99 => _random.NextDouble() * (0.25 - 0.1) + 0.1,
                <= 64.99 => _random.NextDouble() * (0.25 - 0.15) + 0.15,
                <= 69.99 => _random.NextDouble() * (0.3 - 0.2) + 0.2,
                <= 74.99 => _random.NextDouble() * (0.4 - 0.35) + 0.35,
                <= 79.99 => _random.NextDouble() * (0.5 - 0.35) + 0.35,
                <= 84.99 => _random.NextDouble() * (2.0 - 0.9) + 0.9,
                <= 86.99 => _random.NextDouble() * (5.0 - 0.9) + 0.9,
                <= 88.99 => _random.NextDouble() * (10.0 - 5.0) + 5.0,
                <= 91.99 => _random.NextDouble() * (20.0 - 10.0) + 10.0,
                <= 92.99 => _random.NextDouble() * (40.0 - 10.0) + 10.0,
                <= 93.99 => _random.NextDouble() * (60.0 - 40.0) + 40.0,
                <= 95.99 => _random.NextDouble() * (100.0 - 60.0) + 60.0,
                <= 97.99 => _random.NextDouble() * (150.0 - 60.0) + 60.0,
                <= 100.0 => _random.NextDouble() * (175.0 - 150.0) + 150.0,
                _ => 0
            };

            var confirmEmbed = new EmbedBuilder()
                .WithTitle("Confirm Sacrifice")
                .WithDescription($"Are you sure you want to sacrifice your {pokemon.PokemonName} ({ivPercentage:F2}%)?")
                .WithColor(Color.Red)
                .WithFooter("This action cannot be undone!");

            var result = await PromptUserConfirmAsync(confirmEmbed, ctx.User.Id);

            if (!result)
            {
                await ctx.Interaction.SendErrorAsync("Sacrifice cancelled.");
                return;
            }

            await Service.RemoveUserPokemon(ctx.User.Id, pokemon.Id);
            await Service.IncrementSoulGauge(ctx.User.Id, soulIncrement);

            var currentGauge = await Service.GetUserSoulGauge(ctx.User.Id);
            var gaugeDisplay = currentGauge >= 1000
                ? string.Concat(Enumerable.Repeat("<:beambar2:1181749951844319293>", 10))
                : string.Concat(Enumerable.Repeat("<:beambar2:1181749951844319293>", currentGauge / 100)) +
                  "<:beamstart2:1181749967304536134>";

            var responseEmbed = new EmbedBuilder()
                .WithTitle("Pokemon Sacrificed")
                .WithDescription($"You sacrificed your {pokemon.PokemonName} ({ivPercentage:F2}%)\n" +
                                 $"Soul Gauge increased by {soulIncrement:F2}\n" +
                                 $"Current Gauge: {gaugeDisplay}")
                .WithColor(Color.Purple);

            if (currentGauge >= 1000)
                responseEmbed.AddField("Soul Gauge Full!",
                    "You must empty your gauge by communicating with the Ancient Pokemon to collect a reward and to continue collecting souls.\n" +
                    "Use `/meditate` to try to communicate with the Ancient Pokemons spirit.");

            await ctx.Interaction.RespondAsync(embed: responseEmbed.Build());
        }
        catch (Exception ex)
        {
            await ctx.Interaction.SendErrorAsync(ex.Message);
        }
    }

    [ComponentInteraction("pokeinfo:*,*", true)]
    public async Task HandleInfoButtons(string action, string param)
    {
        var interaction = ctx.Interaction as IComponentInteraction;

        switch (action)
        {
            case "more":
                var pokemonId = int.Parse(param);
                var pokemon = pokemonId != 0 ? await Service.GetPokemonById(pokemonId) : null;
                var pokemonName = pokemon?.PokemonName ?? interaction.Message.Embeds.First().Title.Split(' ').Last();

                var forms = await Service.GetPokemonForms(pokemonName);
                var evolutionLine = await Service.GetEvolutionLine(pokemonName);

                var moreInfoEmbed = new EmbedBuilder()
                    .WithColor(0xFF0060);

                // Add forms information
                if (forms == null || !forms.Any() || forms.Contains("None"))
                    moreInfoEmbed.AddField("Available Forms:", "`This Pokemon has no forms.`");
                else
                    moreInfoEmbed.AddField("Available Forms:",
                        $"{string.Join("\n", forms)}\n`/form (form name)`\nor `/mega evolve`");

                // Add evolution line
                if (!string.IsNullOrEmpty(evolutionLine))
                    moreInfoEmbed.AddField("More Info:", $"**Evolution Line**:\n{evolutionLine}");

                var components = new ComponentBuilder()
                    .WithButton("Back", $"pokeinfo:back,{param}")
                    .WithButton("Close", $"pokeinfo:close,{param}", ButtonStyle.Danger, new Emoji("➖"))
                    .Build();

                await interaction.UpdateAsync(msg =>
                {
                    msg.Embed = moreInfoEmbed.Build();
                    msg.Components = components;
                });
                break;

            case "back":
                // Get the original embed from the message
                var originalEmbed = interaction.Message.Embeds.First();
                var backComponents = new ComponentBuilder()
                    .WithButton("More Information", $"pokeinfo:more,{param}", ButtonStyle.Primary, new Emoji("ℹ️"))
                    .Build();

                await interaction.UpdateAsync(msg =>
                {
                    msg.Embed = originalEmbed as Embed;
                    msg.Components = backComponents;
                });
                break;

            case "close":
                await interaction.UpdateAsync(msg =>
                {
                    msg.Content = "Info closed.";
                    msg.Embed = null;
                    msg.Components = null;
                });
                break;
        }
    }

    private async Task ShowUnownedPokedex()
    {
        var pokemonList = await Service.GetAllPokemon();
        var ownedPokemon = await Service.GetUserPokemons(ctx.User.Id);
        var desc = new StringBuilder();
        var pages = new List<PageBuilder>();
        var pokemonCount = 0;

        foreach (var pokemon in pokemonList)
        {
            // Only add if the pokemon is NOT owned
            var isOwned = ownedPokemon.Any(p =>
                p.PokemonName.Equals(pokemon.Identifier, StringComparison.CurrentCultureIgnoreCase));
            if (!isOwned)
            {
                desc.AppendLine($"**{pokemon.Identifier.Titleize()}** - <:delete:1051241645447848009>");
                pokemonCount++;

                if (pokemonCount % 20 == 0)
                {
                    pages.Add(new PageBuilder()
                        .WithColor(Color.Blue)
                        .WithDescription(desc.ToString())
                        .WithTitle($"Unowned Pokemon ({ownedPokemon.Count}/{pokemonList.Count})"));
                    desc.Clear();
                }
            }
        }

        if (desc.Length > 0)
            pages.Add(new PageBuilder()
                .WithColor(Color.Blue)
                .WithDescription(desc.ToString())
                .WithTitle($"Unowned Pokemon ({ownedPokemon.Count}/{pokemonList.Count})"));

        var paginator = new StaticPaginatorBuilder()
            .AddUser(ctx.User)
            .WithPages(pages)
            .WithDefaultEmotes()
            .WithFooter(PaginatorFooter.PageNumber)
            .Build();

        await _interactivity.SendPaginatorAsync(paginator, ctx.Interaction, TimeSpan.FromMinutes(10));
    }

    private async Task DisplayPokemonInfo(Database.Models.PostgreSQL.Pokemon.Pokemon pokemon)
    {
        var pokemonInfo = await Service.GetPokemonInfo(pokemon.PokemonName);
        if (pokemonInfo == null)
        {
            await ctx.Interaction.SendErrorFollowupAsync("Error retrieving pokemon information.");
            return;
        }

        var ivPercentage = (pokemon.HpIv + pokemon.AttackIv + pokemon.DefenseIv +
                            pokemon.SpecialAttackIv + pokemon.SpecialDefenseIv + pokemon.SpeedIv) / 186.0 * 100;

        var embed = new EmbedBuilder()
            .WithTitle($"{pokemon.Name.Titleize()} (Level {pokemon.Level})")
            .WithColor(new Color(Random.Shared.Next(256), Random.Shared.Next(256), Random.Shared.Next(256)))
            .AddField("Types", string.Join(", ", pokemonInfo.Types))
            .AddField("IVs", $"{ivPercentage:F2}%")
            .AddField("Stats",
                $"HP: {pokemonInfo.Stats.Hp}\n" +
                $"Attack: {pokemonInfo.Stats.Attack}\n" +
                $"Defense: {pokemonInfo.Stats.Defense}\n" +
                $"Sp.Atk: {pokemonInfo.Stats.SpecialAttack}\n" +
                $"Sp.Def: {pokemonInfo.Stats.SpecialDefense}\n" +
                $"Speed: {pokemonInfo.Stats.Speed}");

        if (pokemon.Shiny.GetValueOrDefault())
            embed.AddField("✨", "This pokemon is shiny!");

        await ctx.Interaction.FollowupAsync(embed: embed.Build());
    }


    private static string GetTypeEmote(string type)
    {
        return type.ToLower() switch
        {
            "normal" => "<:normal:1061418793416294460>",
            "fire" => "<:fire:1061418789725798430>",
            "water" => "<:water:1061418798549037167>",
            // Add other type emotes here
            _ => type
        };
    }

    private static string GetEggEmote(string eggGroup)
    {
        return eggGroup.ToLower() switch
        {
            "monster" => "<:monster:1061418792191606804>",
            "water1" => "<:water1:1061418796750913586>",
            "bug" => "<:bug:1061418785808072815>",
            // Add other egg group emotes here
            _ => eggGroup
        };
    }

    private static string GetPokemonTitle(string name, bool? shiny, bool? radiant, string skin)
    {
        if (shiny == true) name = "✨ " + name;
        if (radiant == true) name = "🌟 " + name;
        if (!string.IsNullOrEmpty(skin)) name = "💫 " + name;
        return name;
    }

    private async Task DisplayOwnedPokemonInfo(Database.Models.PostgreSQL.Pokemon.Pokemon pokemon)
    {
        var pokemonInfo = await Service.GetPokemonInfo(pokemon.PokemonName);
        if (pokemonInfo == null)
        {
            await ctx.Interaction.SendErrorAsync("Error retrieving pokemon information.");
            return;
        }

        // Calculate stats
        var calculatedStats = await Service.CalculatePokemonStats(pokemon, pokemonInfo.Stats);
        var friendship = Service.CalculateFriendship(pokemon);

        // Calculate IV percentage
        var ivTotal = pokemon.HpIv + pokemon.AttackIv + pokemon.DefenseIv +
                      pokemon.SpecialAttackIv + pokemon.SpecialDefenseIv + pokemon.SpeedIv;
        var ivPercentage = ivTotal / 186.0 * 100;

        var genderEmoji = GetGenderEmoji(pokemon.Gender);

        // Get proper title prefix based on skin
        var titlePrefix = pokemon.Skin switch
        {
            "staff_gif" => "<:staff2:1012753310916296786> ",
            var s when s?.StartsWith("custom/") == true => "<a:custom:1012757910222274560> ",
            _ => ""
        };

        // Get movement bars if it's a special pokemon
        var specEmojis = !string.IsNullOrEmpty(pokemon.Skin)
            ? "<a:spec1:1036851754303770734><a:spec2:1036851753058062376><a:spec3:1036851751023812628>\n"
            : "";

        var embed = new EmbedBuilder()
            .WithTitle(
                $"{titlePrefix}{GetVariantEmoji(pokemon.Shiny, pokemon.Radiant, pokemon.Skin)}{genderEmoji}{pokemon.PokemonName.Titleize()}")
            .WithColor(new Color(_random.Next(256), _random.Next(256), _random.Next(256)))
            .WithFooter(_footers[_random.Next(_footers.Length)]);

        var infoField = new StringBuilder();
        infoField.AppendLine($"**Level**: `{pokemon.Level}`");
        if (!string.IsNullOrEmpty(pokemon.HeldItem))
            infoField.AppendLine($"**Held Item**: `{pokemon.HeldItem}`");
        infoField.AppendLine($"**Types**: {string.Join(", ", pokemonInfo.Types.Select(GetTypeEmote))}");
        infoField.AppendLine($"**Nature**: `{pokemon.Nature}`");
        if (friendship > 0)
            infoField.AppendLine($"**Friendship**: `{friendship}`");
        infoField.AppendLine(specEmojis);

        embed.AddField("Pokemon Information", infoField.ToString());

        // Stats field showing IVs, EVs, and total stats
        var statsField = new StringBuilder();
        statsField.AppendLine(
            $"{PokemonDisplayConstants.HP_DISPLAY} `{pokemon.HpIv}/31` | `{pokemon.HpEv}/252` | `{calculatedStats.MaxHp}`");
        statsField.AppendLine(
            $"{PokemonDisplayConstants.ATK_DISPLAY} `{pokemon.AttackIv}/31` | `{pokemon.AttackEv}/252` | `{calculatedStats.Attack}`");
        statsField.AppendLine(
            $"{PokemonDisplayConstants.DEF_DISPLAY} `{pokemon.DefenseIv}/31` | `{pokemon.DefenseEv}/252` | `{calculatedStats.Defense}`");
        statsField.AppendLine(
            $"{PokemonDisplayConstants.SPATK_DISPLAY} `{pokemon.SpecialAttackIv}/31` | `{pokemon.SpecialAttackEv}/252` | `{calculatedStats.SpecialAttack}`");
        statsField.AppendLine(
            $"{PokemonDisplayConstants.SPDEF_DISPLAY} `{pokemon.SpecialDefenseIv}/31` | `{pokemon.SpecialDefenseEv}/252` | `{calculatedStats.SpecialDefense}`");
        statsField.AppendLine(
            $"{PokemonDisplayConstants.SPE_DISPLAY} `{pokemon.SpeedIv}/31` | `{pokemon.SpeedEv}/252` | `{calculatedStats.Speed}`");

        embed.AddField($"Stats (IV | EV | Total) - {ivPercentage:F2}% IV", statsField.ToString());

        // Get image URL
        var (form, imageUrl) = await Service.GetPokemonFormInfo(pokemon.PokemonName, pokemon.Shiny ?? false,
            pokemon.Radiant ?? false, pokemon.Skin);
        if (!string.IsNullOrEmpty(imageUrl)) embed.WithImageUrl(imageUrl);

        var components = new ComponentBuilder()
            .WithButton("More Information", $"pokeinfo:more,{pokemon.Id}", ButtonStyle.Primary, new Emoji("ℹ️"))
            .Build();

        await ctx.Interaction.RespondAsync(embed: embed.Build(), components: components);
    }


    private string GetVariantEmoji(bool? shiny, bool? radiant, string skin)
    {
        if (skin?.ToLower() == "shadow") return "💫";
        if (radiant == true) return "🌟";
        if (shiny == true) return "✨";
        return "";
    }

    private string GetGenderEmoji(string gender)
    {
        return gender?.ToLower() switch
        {
            "m" or "male" => "♂️",
            "f" or "female" => "♀️",
            _ => ""
        };
    }

    public enum PokemonVariantType
    {
        Shiny,
        Radiant,
        Shadow
    }
}