/*using System.Text.Json;
using Ditto.Database.DbContextStuff;
using Ditto.Services.Impl;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Ditto.Services;

public class PokemonCreationService : IPokemonCreationService
{
    private readonly Random _rng = new();
    private readonly DbContextProvider _db;
    private readonly IMongoService _mongo;
    private readonly RedisService _redis;

    private static readonly string[] NatureList =
    [
        "Lonely", "Brave", "Adamant", "Naughty", "Bold",
        "Relaxed", "Impish", "Lax", "Timid", "Hasty",
        "Jolly", "Naive", "Modest", "Mild", "Quiet",
        "Rash", "Calm", "Gentle", "Sassy", "Careful",
        "Bashful", "Quirky", "Serious", "Docile", "Hardy"
    ];

    private static readonly string[] DefaultMoves = ["tackle", "tackle", "tackle", "tackle"];

    public PokemonCreationService(DbContextProvider db, IMongoService mongo, RedisService redis)
    {
        _db = db;
        _mongo = mongo;
        _redis = redis;
    }

    public async Task<bool> RemovePokemon(ulong userId, int pokemonId, bool delete = false)
    {
        await using var ctx = await _db.GetContextAsync();

        var data = await ctx.Users
            .Where(u => u.UId == userId)
            .Select(u => new { u.Pokes, u.Selected, u.Parties })
            .FirstOrDefaultAsync();

        if (data == null)
            throw new UserNotStartedException();

        var pokes = data.Pokes?.ToList() ?? [];
        if (!pokes.Contains(pokemonId))
            return false;

        var selected = data.Selected == pokemonId ? null : data.Selected;
        var party = data.Parties.Where(p => p.PId == pokemonId).ToArray();

        await ctx.Users
            .Where(u => u.UId == userId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.Pokes, pokes.Where(p => p != pokemonId).ToArray())
                .SetProperty(x => x.Selected, selected)
                .SetProperty(x => x.Parties, party));

        if (delete)
            await ctx.Pokemon.Where(p => p.Id == pokemonId).ExecuteDeleteAsync();
        else
            await ctx.Pokemon.Where(p => p.Id == pokemonId)
                .ExecuteUpdateAsync(p => p.SetProperty(x => x.Fav, false));

        return true;
    }

    private async Task<bool> CheckShadowHunt(ulong userId, string pokemon)
    {
        await using var ctx = await _db.GetContextAsync();
        var data = await ctx.Users
            .Where(u => u.UId == userId)
            .Select(u => new { u.Hunt, u.Chain })
            .FirstOrDefaultAsync();

        if (data == null)
            return false;

        if (data.Hunt != pokemon)
            return false;

        var makeShadow = _rng.NextDouble() < 1.0 / 6000 * Math.Pow(4, data.Chain / 1000.0);

        if (makeShadow)
            await ctx.Users.Where(u => u.UId == userId)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.Chain, 0));
        else
            await ctx.Users.Where(u => u.UId == userId)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.Chain, x.Chain + 1));

        return makeShadow;
    }

    public async Task<Pokes> CreatePokemon(
        ulong userId,
        string pokemonName,
        bool shiny = false,
        bool boosted = false,
        bool radiant = false,
        string skin = null,
        string gender = null,
        int level = 1)
    {
        var formInfo = await _mongo.Forms.Find(f => f.Identifier.Equals(pokemonName, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefaultAsync();
        if (formInfo == null)
            return null;

        var pokemonInfo = await _mongo.PFile.Find(p => p.Id == formInfo.PokemonId).FirstOrDefaultAsync();
        if (pokemonInfo == null)
            return null;

        var genderRate = pokemonInfo.GenderRate;

        var abilityIds = await _mongo.PokeAbilities
            .Find(a => a.PokemonId == formInfo.PokemonId)
            .ToListAsync();

        if (skin != null)
        {
            shiny = false;
            radiant = false;
            skin = skin.ToLower();
        }
        else if (radiant)
        {
            shiny = false;
        }
        else if (!shiny)
        {
            var shadowCheck = await CheckShadowHunt(userId, pokemonName);
            if (shadowCheck)
            {
                skin = "shadow";
                // Log shadow spawn logic here if needed
            }
        }

        // Generate IVs
        var minIv = boosted ? 12 : 1;
        var maxIv = boosted || _rng.Next(2) == 0 ? 31 : 29;

        var nature = NatureList[_rng.Next(NatureList.Length)];

        if (gender == null)
        {
            gender = pokemonName.ToLower() switch
            {
                var p when p.Contains("nidoran-") => pokemonName[^2..],
                "illumise" => "-f",
                "volbeat" => "-m",
                _ when genderRate == -1 => "-x",
                _ => _rng.Next(8) < genderRate ? "-f" : "-m"
            };
        }

        var poke = new Pokes
        {
            Pokname = pokemonName,
            Hpiv = _rng.Next(minIv, maxIv + 1),
            Atkiv = _rng.Next(minIv, maxIv + 1),
            Defiv = _rng.Next(minIv, maxIv + 1),
            Spatkiv = _rng.Next(minIv, maxIv + 1),
            Spdefiv = _rng.Next(minIv, maxIv + 1),
            Speediv = _rng.Next(minIv, maxIv + 1),
            Hpev = 0,
            Atkev = 0,
            Defev = 0,
            Spatkev = 0,
            Spdefev = 0,
            Speedev = 0,
            Pokelevel = level,
            Moves = DefaultMoves,
            Hitem = "None",
            Exp = 1,
            Nature = nature,
            Expcap = level * level,
            Poknick = "None",
            Shiny = shiny,
            Price = 0,
            MarketEnlist = false,
            Fav = false,
            AbilityIndex = _rng.Next(abilityIds.Count),
            Gender = gender,
            CaughtBy = userId,
            Radiant = radiant,
            Skin = skin,
            Owner = userId,
            Name = pokemonName,
            TimeStamp = DateTime.UtcNow
        };

        await using var ctx = await _db.GetContextAsync();
        await ctx.Pokemon.AddAsync(poke);
        await ctx.SaveChangesAsync();

        await ctx.Users
            .Where(u => u.UId == userId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.Pokes, u => u.Pokes.Append(poke.Id).ToArray()));

        return new Pokes
        {
            Id = poke.Id,
            Gender = gender,
            IvSum = poke.Hpiv + poke.Atkiv + poke.Defiv + poke.Spatkiv + poke.Spdefiv + poke.Speediv,
            Emoji = GetEmoji(shiny: shiny, radiant: radiant, skin: skin)
        };
    }

    private static string GetEmoji(bool shiny, bool radiant, string skin)
    {
        // Add your emoji logic here based on shiny/radiant/skin
        return "";
    }
}*/

