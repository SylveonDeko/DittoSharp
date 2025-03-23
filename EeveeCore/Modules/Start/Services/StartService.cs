using EeveeCore.Database.Models.PostgreSQL.Bot;
using EeveeCore.Database.Models.PostgreSQL.Pokemon;
using EeveeCore.Modules.Spawn.Services;
using Microsoft.EntityFrameworkCore;

namespace EeveeCore.Modules.Start.Services;

public class StartService(EeveeCoreContext db, DiscordShardedClient client, SpawnService svc) : INService
{
    /// <summary>
    ///     Handles creating a new user account
    /// </summary>
    public async Task<(bool Success, string Message)> RegisterNewUser(ulong userId)
    {
        // Check if user already exists
        var exists = await db.Users.AnyAsync(u => u.UserId == userId);
        if (exists) return (false, "You have already registered");

        // Create new user
        var newUser = new User
        {
            UserId = userId,
            Redeems = 0,
            EvPoints = 0,
            TrainerNickname = null,
            UpvotePoints = 0,
            MewCoins = 0,
            UserOrder = "kek",
            Pokemon = [],
            Visible = true,
            Inventory = "{\"nature-capsules\" : 5, \"honey\" : 1, \"battle-multiplier\": 1, \"shiny-multiplier\": 0 }",
            Comp = true,
            Party = [0, 0, 0, 0, 0, 0]
        };

        db.Users.Add(newUser);

        // Create achievement record
        await db.Achievements.AddAsync(new Achievement
        {
            UserId = userId
        });

        // Create egg hatchery entries for user (3 groups)
        for (short group = 1; group <= 3; group++)
            await db.EggHatcheries.AddAsync(new EggHatchery
            {
                UserId = userId,
                Group = group
            });

        await db.SaveChangesAsync();
        return (true, "Successfully registered new user");
    }

    /// <summary>
    ///     Creates a starter Pokemon for a user
    /// </summary>
    public async Task<(bool Success, string Message)> CreateStarterPokemon(ulong userId, string? starterName)
    {
        try
        {
            await svc.CreatePokemon(userId, starterName, boosted: true);

            return (true, $"Successfully created starter Pokémon {starterName}");
        }
        catch (Exception ex)
        {
            // Log error to channel
            if (client.GetChannel(1351696540065857597) is IMessageChannel errorChannel)
                await errorChannel.SendMessageAsync($"{userId} failed to create starter: {ex.Message}");

            return (false, "Failed to create starter Pokémon. Please try again.");
        }
    }

    /// <summary>
    ///     Gets grass starter options
    /// </summary>
    public List<SelectMenuOptionBuilder> GetGrassStarterOptions()
    {
        return
        [
            new SelectMenuOptionBuilder().WithLabel("Bulbasaur").WithValue("Bulbasaur")
                .WithDescription("Grass-type Starter").WithEmote(new Emoji("🍃")),
            new SelectMenuOptionBuilder().WithLabel("Chikorita").WithValue("Chikorita")
                .WithDescription("Grass-type Starter").WithEmote(new Emoji("🍃")),
            new SelectMenuOptionBuilder().WithLabel("Treecko").WithValue("Treecko")
                .WithDescription("Grass-type Starter")
                .WithEmote(new Emoji("🍃")),
            new SelectMenuOptionBuilder().WithLabel("Turtwig").WithValue("Turtwig")
                .WithDescription("Grass-type Starter")
                .WithEmote(new Emoji("🍃")),
            new SelectMenuOptionBuilder().WithLabel("Snivy").WithValue("Snivy").WithDescription("Grass-type Starter")
                .WithEmote(new Emoji("🍃")),
            new SelectMenuOptionBuilder().WithLabel("Chespin").WithValue("Chespin")
                .WithDescription("Grass-type Starter")
                .WithEmote(new Emoji("🍃")),
            new SelectMenuOptionBuilder().WithLabel("Rowlet").WithValue("Rowlet").WithDescription("Grass-type Starter")
                .WithEmote(new Emoji("🍃")),
            new SelectMenuOptionBuilder().WithLabel("Grookey").WithValue("Grookey")
                .WithDescription("Grass-type Starter")
                .WithEmote(new Emoji("🍃")),
            new SelectMenuOptionBuilder().WithLabel("Sprigatito").WithValue("Sprigatito")
                .WithDescription("Grass-type Starter").WithEmote(new Emoji("🍃"))
        ];
    }

    /// <summary>
    ///     Gets water starter options
    /// </summary>
    public List<SelectMenuOptionBuilder> GetWaterStarterOptions()
    {
        return
        [
            new SelectMenuOptionBuilder().WithLabel("Squirtle").WithValue("Squirtle")
                .WithDescription("Water-type Starter")
                .WithEmote(new Emoji("💧")),
            new SelectMenuOptionBuilder().WithLabel("Totodile").WithValue("Totodile")
                .WithDescription("Water-type Starter")
                .WithEmote(new Emoji("💧")),
            new SelectMenuOptionBuilder().WithLabel("Mudkip").WithValue("Mudkip").WithDescription("Water-type Starter")
                .WithEmote(new Emoji("💧")),
            new SelectMenuOptionBuilder().WithLabel("Piplup").WithValue("Piplup").WithDescription("Water-type Starter")
                .WithEmote(new Emoji("💧")),
            new SelectMenuOptionBuilder().WithLabel("Oshawott").WithValue("Oshawott")
                .WithDescription("Water-type Starter")
                .WithEmote(new Emoji("💧")),
            new SelectMenuOptionBuilder().WithLabel("Froakie").WithValue("Froakie")
                .WithDescription("Water-type Starter")
                .WithEmote(new Emoji("💧")),
            new SelectMenuOptionBuilder().WithLabel("Popplio").WithValue("Popplio")
                .WithDescription("Water-type Starter")
                .WithEmote(new Emoji("💧")),
            new SelectMenuOptionBuilder().WithLabel("Sobble").WithValue("Sobble").WithDescription("Water-type Starter")
                .WithEmote(new Emoji("💧")),
            new SelectMenuOptionBuilder().WithLabel("Quaxly").WithValue("Quaxly").WithDescription("Water-type Starter")
                .WithEmote(new Emoji("💧"))
        ];
    }

    /// <summary>
    ///     Gets fire starter options
    /// </summary>
    public List<SelectMenuOptionBuilder> GetFireStarterOptions()
    {
        return
        [
            new SelectMenuOptionBuilder().WithLabel("Charmander").WithValue("Charmander")
                .WithDescription("Fire-type Starter").WithEmote(new Emoji("🔥")),
            new SelectMenuOptionBuilder().WithLabel("Cyndaquil").WithValue("Cyndaquil")
                .WithDescription("Fire-type Starter")
                .WithEmote(new Emoji("🔥")),
            new SelectMenuOptionBuilder().WithLabel("Torchic").WithValue("Torchic").WithDescription("Fire-type Starter")
                .WithEmote(new Emoji("🔥")),
            new SelectMenuOptionBuilder().WithLabel("Chimchar").WithValue("Chimchar")
                .WithDescription("Fire-type Starter")
                .WithEmote(new Emoji("🔥")),
            new SelectMenuOptionBuilder().WithLabel("Tepig").WithValue("Tepig").WithDescription("Fire-type Starter")
                .WithEmote(new Emoji("🔥")),
            new SelectMenuOptionBuilder().WithLabel("Fennekin").WithValue("Fennekin")
                .WithDescription("Fire-type Starter")
                .WithEmote(new Emoji("🔥")),
            new SelectMenuOptionBuilder().WithLabel("Litten").WithValue("Litten").WithDescription("Fire-type Starter")
                .WithEmote(new Emoji("🔥")),
            new SelectMenuOptionBuilder().WithLabel("Scorbunny").WithValue("Scorbunny")
                .WithDescription("Fire-type Starter")
                .WithEmote(new Emoji("🔥")),
            new SelectMenuOptionBuilder().WithLabel("Fuecoco").WithValue("Fuecoco").WithDescription("Fire-type Starter")
                .WithEmote(new Emoji("🔥"))
        ];
    }
}