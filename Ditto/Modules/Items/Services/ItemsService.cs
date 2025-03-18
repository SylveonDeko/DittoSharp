using System.Text.Json;
using Ditto.Database.DbContextStuff;
using Ditto.Database.Models.PostgreSQL.Game;
using Ditto.Modules.Pokemon.Services;
using Ditto.Services.Impl;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Serilog;

namespace Ditto.Modules.Items.Services;

public class ItemsService(
    IMongoService mongoDb,
    DbContextProvider dbContextProvider,
    PokemonService pokemonService,
    IDataCache cache)
    : INService
{
    private readonly Random _random = new();
    private readonly IDataCache _cache = cache;

    private const int MaxMarketSlots = 10;
    private readonly HashSet<string> _activeItemList =
    [
        "fire-stone", "water-stone", "thunder-stone", "leaf-stone", "moon-stone",
        "sun-stone", "shiny-stone", "dusk-stone", "dawn-stone", "ice-stone", "black-augurite",
        "kings-rock", "metal-coat", "dragon-scale", "upgrade", "protector", "electirizer",
        "magmarizer", "dubious-disc", "reaper-cloth", "oval-stone", "razor-claw",
        "razor-fang", "prism-scale", "whipped-dream", "sachet", "strawberry-sweet",
        "love-sweet", "berry-sweet", "clover-sweet", "flower-sweet", "star-sweet",
        "ribbon-sweet", "sweet-apple", "tart-apple", "cracked-pot", "chipped-pot",
        "galarica-cuff", "galarica-wreath", "auspicious-armor", "malicious-armor",
        "gimmighoul-coin", "deep-sea-scale", "deep-sea-tooth", "friendship-stone"
    ];

    private readonly HashSet<string> _berryList =
    [
        "cheri-berry", "chesto-berry", "pecha-berry", "rawst-berry", "aspear-berry",
        "leppa-berry", "oran-berry", "persim-berry", "lum-berry", "sitrus-berry",
        "figy-berry", "wiki-berry", "mago-berry", "aguav-berry", "iapapa-berry",
        "razz-berry", "bluk-berry", "nanab-berry", "wepear-berry", "pinap-berry",
        "pomeg-berry", "kelpsy-berry", "qualot-berry", "hondew-berry", "grepa-berry",
        "tamato-berry", "cornn-berry", "magost-berry", "rabuta-berry", "nomel-berry",
        "spelon-berry", "pamtre-berry", "watmel-berry", "durin-berry", "belue-berry",
        "occa-berry", "passho-berry", "wacan-berry", "rindo-berry", "yache-berry",
        "chople-berry", "kebia-berry", "shuca-berry", "coba-berry", "payapa-berry",
        "tanga-berry", "charti-berry", "kasib-berry", "haban-berry", "colbur-berry",
        "babiri-berry", "chilan-berry", "liechi-berry", "ganlon-berry", "salac-berry",
        "petaya-berry", "apicot-berry", "lansat-berry", "starf-berry", "enigma-berry",
        "micle-berry", "custap-berry", "jaboca-berry", "rowap-berry", "roseli-berry",
        "kee-berry", "maranga-berry"
    ];

    private bool IsFormed(string pokemonName)
    {
        return pokemonName.EndsWith("-mega") || pokemonName.EndsWith("-x") || pokemonName.EndsWith("-y") ||
               pokemonName.EndsWith("-origin") || pokemonName.EndsWith("-10") || pokemonName.EndsWith("-complete") ||
               pokemonName.EndsWith("-ultra") || pokemonName.EndsWith("-crowned") || pokemonName.EndsWith("-eternamax") ||
               pokemonName.EndsWith("-blade");
    }

    public async Task<(bool Success, string Message, string HeldItem, string PokemonName, Dictionary<string, int> Items)>
        PrepItemRemove(ulong userId)
    {
        await using var db = await dbContextProvider.GetContextAsync();

        var data = await (
            from poke in db.UserPokemon
            join user in db.Users on poke.Id equals user.Selected
            where user.UserId == userId
            select new { poke.HeldItem, poke.PokemonName, user.Items }
        ).FirstOrDefaultAsyncEF();

        if (data == null)
        {
            return (false, "You do not have a pokemon selected!\nSelect one with `/select` first.", null, null, null);
        }

        var items = JsonSerializer.Deserialize<Dictionary<string, int>>(data.Items ?? "{}") ?? new Dictionary<string, int>();

        if (data.HeldItem.ToLower() is "none" or null)
        {
            return (false, "Your selected Pokemon is not holding any item!", null, null, null);
        }

        if ((data.HeldItem is "megastone" or "mega-stone" or "mega-stone-x" or "mega-stone-y") &&
            (data.PokemonName.EndsWith("-mega") || data.PokemonName.EndsWith("-x") || data.PokemonName.EndsWith("-y")))
        {
            return (false, "Deform this Pokemon before Unequipping the item!", null, null, null);
        }

        if (IsFormed(data.PokemonName) && data.HeldItem is "primal-orb" or "blue-orb" or "red-orb" or
            "griseous-orb" or "ultranecronium-z" or "rusty-sword" or "rusty-shield")
        {
            return (false, "Deform this Pokemon before Unequipping the item!", null, null, null);
        }

        return (true, null, data.HeldItem, data.PokemonName, items);
    }

    public async Task<CommandResult> Unequip(ulong userId)
    {
        var prepResult = await PrepItemRemove(userId);
        if (!prepResult.Success)
        {
            return new CommandResult { Message = prepResult.Message };
        }

        await using var db = await dbContextProvider.GetContextAsync();
        prepResult.Items[prepResult.HeldItem] = prepResult.Items.GetValueOrDefault(prepResult.HeldItem, 0) + 1;

        var serializedItems = JsonSerializer.Serialize(prepResult.Items);

        await db.Users.Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.Items, serializedItems));

        await db.UserPokemon.Where(p => p.Id == (
                db.Users.Where(u => u.UserId == userId)
                    .Select(u => u.Selected)
                    .FirstOrDefault()
            ))
            .ExecuteUpdateAsync(p => p
                .SetProperty(x => x.HeldItem, "None"));

        return new CommandResult { Message = $"Successfully unequipped a {prepResult.HeldItem} from selected Pokemon" };
    }

    public async Task<CommandResult> Drop(ulong userId)
    {
        var prepResult = await PrepItemRemove(userId);
        if (!prepResult.Success)
        {
            return new CommandResult { Message = prepResult.Message };
        }

        await using var db = await dbContextProvider.GetContextAsync();
        await db.UserPokemon.Where(p => p.Id == (
                db.Users.Where(u => u.UserId == userId)
                    .Select(u => u.Selected)
                    .FirstOrDefault()
            ))
            .ExecuteUpdateAsync(p => p
                .SetProperty(x => x.HeldItem, "None"));

        return new CommandResult { Message = $"Successfully Dropped the {prepResult.HeldItem}" };
    }

    public async Task<CommandResult> Transfer(ulong userId, int pokemonNumber)
    {
        var prepResult = await PrepItemRemove(userId);
        if (!prepResult.Success)
        {
            return new CommandResult { Message = prepResult.Message };
        }

        await using var db = await dbContextProvider.GetContextAsync();
        var user = await db.Users.FirstOrDefaultAsyncEF(u => u.UserId == userId);
        if (user == null || pokemonNumber <= 0 || pokemonNumber > user.Pokemon.Length)
        {
            return new CommandResult { Message = "You do not have that Pokemon!" };
        }

        var targetPokemon = await db.UserPokemon.FirstOrDefaultAsyncEF(p => p.Id == user.Pokemon[pokemonNumber - 1]);
        if (targetPokemon == null)
        {
            return new CommandResult { Message = "That Pokemon does not exist!" };
        }

        if (targetPokemon.HeldItem.ToLower() != "none")
        {
            return new CommandResult { Message = "That Pokemon is already holding an item" };
        }

        await db.UserPokemon.Where(p => p.Id == user.Selected)
            .ExecuteUpdateAsync(p => p
                .SetProperty(x => x.HeldItem, "None"));

        await db.UserPokemon.Where(p => p.Id == targetPokemon.Id)
            .ExecuteUpdateAsync(p => p
                .SetProperty(x => x.HeldItem, prepResult.HeldItem));

        return new CommandResult
        {
            Message = $"You have successfully transfered the {prepResult.HeldItem} from your {prepResult.PokemonName} to your {targetPokemon.PokemonName}!"
        };
    }

    public async Task<CommandResult> Equip(ulong userId, string itemName)
    {
        itemName = string.Join("-", itemName.Split()).ToLower();

        var itemInfo = await mongoDb.Shop.Find(x => x.Item == itemName).FirstOrDefaultAsync();

        if (itemName == "nature-capsule")
        {
            return new CommandResult { Message = "Use `/change nature` to use a nature capsule to change your pokemon's nature." };
        }

        if (!_berryList.Contains(itemName) && itemInfo == null && itemName != "glitchy-orb")
        {
            return new CommandResult { Message = "That Item does not exist!" };
        }

        if (_activeItemList.Contains(itemName))
        {
            return new CommandResult { Message = $"That item cannot be equipped! Use it on your poke with `/apply {itemName}`." };
        }

        await using var db = await dbContextProvider.GetContextAsync();
        var user = await db.Users.FirstOrDefaultAsyncEF(u => u.UserId == userId);
        if (user == null)
        {
            return new CommandResult { Message = "You have not started!\nStart with `/start` first." };
        }

        var items = JsonSerializer.Deserialize<Dictionary<string, int>>(user.Items ?? "{}") ?? new();
        if (items.GetValueOrDefault(itemName, 0) == 0)
        {
            return new CommandResult { Message = $"You do not have any {itemName}!" };
        }

        var selectedPokemon = await db.UserPokemon.FirstOrDefaultAsyncEF(p => p.Id == user.Selected);
        if (selectedPokemon == null)
        {
            return new CommandResult { Message = "You do not have a pokemon selected!\nSelect one with `/select` first." };
        }

        if (selectedPokemon.HeldItem.ToLower() != "none")
        {
            return new CommandResult { Message = $"Your pokemon is already holding the {selectedPokemon.HeldItem}! Unequip it with `/unequip` first!" };
        }

        items[itemName]--;

        string? serializedItems;
        if (itemName == "glitchy-orb")
        {
            if (selectedPokemon.Shiny.GetValueOrDefault() || selectedPokemon.Radiant.GetValueOrDefault() || selectedPokemon.Skin != null)
            {
                return new CommandResult { Message = "Please select a pokemon which is not **Shiny or Radiant**, and does not have a **Skin** attached." };
            }

            await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                .ExecuteUpdateAsync(p => p
                    .SetProperty(x => x.Skin, "glitch"));

            serializedItems = JsonSerializer.Serialize(items);

            await db.Users.Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.Items, serializedItems));

            return new CommandResult { Message = "The orb disappears - something seems off about your pokemon..." };
        }

        if (itemName == "ability-capsule")
        {
            var formInfo = await mongoDb.Forms.Find(f => f.Identifier == selectedPokemon.PokemonName.ToLower())
                .FirstOrDefaultAsync();

            var abilityIds = await mongoDb.PokeAbilities
                .Find(a => a.PokemonId == formInfo.PokemonId)
                .ToListAsync();

            if (abilityIds.Count <= 1)
            {
                return new CommandResult { Message = "That Pokemon cannot have its ability changed!" };
            }

            var newIndex = (selectedPokemon.AbilityIndex + 1) % abilityIds.Count;
            var newAbilityId = abilityIds[newIndex].AbilityId;

            await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                .ExecuteUpdateAsync(p => p
                    .SetProperty(x => x.AbilityIndex, newIndex));

            var newAbility = await mongoDb.Abilities.Find(a => a.AbilityId == newAbilityId).FirstOrDefaultAsync();

            serializedItems = JsonSerializer.Serialize(items);

            await db.Users.Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.Items, serializedItems));

            return new CommandResult { Message = $"You have Successfully changed your Pokémon's ability to {newAbility.Identifier}" };
        }

        if (itemName == "daycare-space")
        {
            serializedItems = JsonSerializer.Serialize(items);

            await db.Users.Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.Items, serializedItems)
                    .SetProperty(x => x.DaycareLimit, u => u.DaycareLimit + 1));

            return new CommandResult { Message = "You have successfully equipped an Extra Daycare Space!" };
        }

        if (itemName == "ev-reset")
        {
            serializedItems = JsonSerializer.Serialize(items);

            await db.Users.Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.Items, serializedItems));

            await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                .ExecuteUpdateAsync(p => p
                    .SetProperty(x => x.HpEv, 0)
                    .SetProperty(x => x.AttackEv, 0)
                    .SetProperty(x => x.DefenseEv, 0)
                    .SetProperty(x => x.SpecialAttackEv, 0)
                    .SetProperty(x => x.SpecialDefenseEv, 0)
                    .SetProperty(x => x.SpeedEv, 0));

            return new CommandResult { Message = "You have successfully reset the Effort Values (EVs) of your selected Pokemon!" };
        }

        if (itemName.EndsWith("-rod"))
        {
            var fishingLevel = await db.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.FishingLevel)
                .FirstOrDefaultAsyncEF();

            if (itemName == "supreme-rod" && fishingLevel < 105)
            {
                return new CommandResult { Message = "You need to be fishing level 105 to use this item!" };
            }
            if (itemName == "epic-rod" && fishingLevel < 150)
            {
                return new CommandResult { Message = "You need to be fishing level 150 to use this item!" };
            }
            if (itemName == "master-rod" && fishingLevel < 200)
            {
                return new CommandResult { Message = "You need to be fishing level 200 to use this item!" };
            }

            serializedItems = JsonSerializer.Serialize(items);

            await db.Users.Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.Items, serializedItems)
                    .SetProperty(x => x.HeldItem, itemName));

            return new CommandResult { Message = $"You have successfully equipped your {itemName}" };
        }

        if (itemName is "zinc" or "hp-up" or "protein" or "calcium" or "iron" or "carbos")
        {
            try
            {
                var updated = itemName switch
                {
                    // Validate itemName and set the appropriate property update
                    "calcium" => await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                        .ExecuteUpdateAsync(p => p.SetProperty(x => x.SpecialAttackEv, x => x.SpecialAttackEv + 10)),
                    "carbos" => await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                        .ExecuteUpdateAsync(p => p.SetProperty(x => x.SpeedEv, x => x.SpeedEv + 10)),
                    "hp-up" => await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                        .ExecuteUpdateAsync(p => p.SetProperty(x => x.HpEv, x => x.HpEv + 10)),
                    "iron" => await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                        .ExecuteUpdateAsync(p => p.SetProperty(x => x.DefenseEv, x => x.DefenseEv + 10)),
                    "protein" => await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                        .ExecuteUpdateAsync(p => p.SetProperty(x => x.AttackEv, x => x.AttackEv + 10)),
                    "zinc" => await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                        .ExecuteUpdateAsync(p => p.SetProperty(x => x.SpecialDefenseEv, x => x.SpecialDefenseEv + 10)),
                    _ => throw new ArgumentException("Invalid vitamin type")
                };

                if (updated == 0)
                {
                    return new CommandResult { Message = "Your Pokemon has maxed all 510 EVs" };
                }

                serializedItems = JsonSerializer.Serialize(items);

                await db.Users.Where(u => u.UserId == userId)
                    .ExecuteUpdateAsync(u => u
                        .SetProperty(x => x.Items, serializedItems));

                return new CommandResult { Message = $"You have successfully used your {itemName}" };
            }
            catch
            {
                return new CommandResult { Message = "Your Pokemon has maxed all 510 EVs" };
            }
        }

        await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
            .ExecuteUpdateAsync(p => p
                .SetProperty(x => x.HeldItem, itemName));

        serializedItems = JsonSerializer.Serialize(items);

        await db.Users.Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.Items, serializedItems));
        var evolveResult = await pokemonService.TryEvolve(selectedPokemon.Id);

        return new CommandResult { Message = $"You have successfully given your selected Pokemon a {itemName}" };
    }

    public async Task<CommandResult> Apply(ulong userId, string itemName, IMessageChannel channel)
    {
        itemName = string.Join("-", itemName.Split()).ToLower();

        if (itemName == "nature-capsule")
        {
            return new CommandResult { Message = "Use `/change nature` to use a nature capsule to change your pokemon's nature." };
        }

        if (!_activeItemList.Contains(itemName))
        {
            return new CommandResult { Message = $"That item cannot be used on a poke! Try equipping it with `/equip {itemName}`." };
        }

        await using var db = await dbContextProvider.GetContextAsync();
        var user = await db.Users.FirstOrDefaultAsyncEF(u => u.UserId == userId);
        if (user == null)
        {
            return new CommandResult { Message = "You have not started!\nStart with `/start` first." };
        }

        var items = JsonSerializer.Deserialize<Dictionary<string, int>>(user.Items ?? "{}") ?? new();
        if (items.GetValueOrDefault(itemName, 0) == 0)
        {
            return new CommandResult { Message = $"You do not have any {itemName}!" };
        }

        var selectedPokemon = await db.UserPokemon.FirstOrDefaultAsyncEF(p => p.Id == user.Selected);
        if (selectedPokemon == null)
        {
            return new CommandResult { Message = "You do not have a pokemon selected!\nSelect one with `/select` first." };
        }

        items[itemName]--;

        string? serializedItems;
        if (itemName == "friendship-stone")
        {
            await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                .ExecuteUpdateAsync(p => p
                    .SetProperty(x => x.Happiness, x => x.Happiness + 300));

            serializedItems = JsonSerializer.Serialize(items);

            await db.Users.Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.Items, serializedItems));

            return new CommandResult { Message = $"Your {itemName} was consumed!" };
        }

        var evolveResult = await pokemonService.TryEvolve(selectedPokemon.Id, itemName);
        if (!evolveResult.Success)
        {
            return new CommandResult { Message = $"The {itemName} had no effect!" };
        }

        serializedItems = JsonSerializer.Serialize(items);

        await db.Users.Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.Items, serializedItems));

        return new CommandResult { Message = $"Your {itemName} was consumed!" };
    }

    public async Task<CommandResult> BuyItem(ulong userId, string itemName)
    {
        itemName = itemName.Replace(" ", "-").ToLower();

        if (itemName == "daycare-space")
        {
            return new CommandResult { Message = "Use `/buy daycare`, not `/buy item daycare-space`." };
        }

        var item = await mongoDb.Shop.Find(x => x.Item == itemName).FirstOrDefaultAsync();
        if (item == null)
        {
            return new CommandResult { Message = "That Item is not in the market" };
        }

        var price = (ulong)item.Price;
        await using var db = await dbContextProvider.GetContextAsync();

        var data = await (
            from user in db.Users
            where user.UserId == userId
            select new { user.Items, user.MewCoins, user.Selected }
        ).FirstOrDefaultAsyncEF();

        if (data == null)
        {
            return new CommandResult { Message = "You have not started!\nStart with `/start` first." };
        }

        if (data.MewCoins < price)
        {
            return new CommandResult { Message = $"You don't have {price} credits!" };
        }

        if (itemName == "market-space")
        {
            if (data.MewCoins < 30000)
            {
                return new CommandResult { Message = $"You need 30,000 credits to buy a market space! You only have {data.MewCoins}..." };
            }

            var marketLimit = await db.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.MarketLimit)
                .FirstOrDefaultAsyncEF();

            if (marketLimit >= MaxMarketSlots)
            {
                return new CommandResult { Message = "You already have the maximum number of market spaces!" };
            }

            await db.Users.Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.MarketLimit, x => x.MarketLimit + 1)
                    .SetProperty(x => x.MewCoins, x => x.MewCoins - 30000));

            return new CommandResult { Message = "You have successfully bought an extra market space!" };
        }

        if (itemName.EndsWith("-rod"))
        {
            var fishingLevel = await db.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.FishingLevel)
                .FirstOrDefaultAsyncEF();

            if (itemName == "supreme-rod" && fishingLevel < 105)
            {
                return new CommandResult { Message = "You need to be fishing level 105 to use this item!" };
            }
            if (itemName == "epic-rod" && fishingLevel < 150)
            {
                return new CommandResult { Message = "You need to be fishing level 150 to use this item!" };
            }
            if (itemName == "master-rod" && fishingLevel < 200)
            {
                return new CommandResult { Message = "You need to be fishing level 200 to use this item!" };
            }

            var items = JsonSerializer.Deserialize<Dictionary<string, int>>(data.Items ?? "{}") ?? new();
            items[itemName] = items.GetValueOrDefault(itemName, 0) + 1;

            var serializedItems = JsonSerializer.Serialize(items);

            await db.Users.Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.MewCoins, x => x.MewCoins - price)
                    .SetProperty(x => x.Items, serializedItems));

            return new CommandResult { Message = $"You have successfully bought the {itemName}!" };
        }

        if (_activeItemList.Contains(itemName))
        {
            var items = JsonSerializer.Deserialize<Dictionary<string, int>>(data.Items ?? "{}") ?? new();
            items[itemName] = items.GetValueOrDefault(itemName, 0) + 1;

            var serializedItems = JsonSerializer.Serialize(items);

            await db.Users.Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.MewCoins, x => x.MewCoins - price)
                    .SetProperty(x => x.Items, serializedItems));

            return new CommandResult { Message = $"You have successfully bought a {itemName}! Use it with `/apply {itemName}`." };
        }

        if (data.Selected == null)
        {
            return new CommandResult
            {
                Message = "You do not have a selected pokemon and the item you are trying to buy requires one!\nUse `/select` to select a pokemon."
            };
        }

        var selectedPokemon = await db.UserPokemon.FirstOrDefaultAsyncEF(p => p.Id == data.Selected);
        if (selectedPokemon == null)
        {
            return new CommandResult { Message = "Selected pokemon not found!" };
        }

        if (itemName == "ability-capsule")
        {
            var formInfo = await mongoDb.Forms.Find(f => f.Identifier == selectedPokemon.PokemonName.ToLower())
                .FirstOrDefaultAsync();

            var abilityIds = await mongoDb.PokeAbilities
                .Find(a => a.PokemonId == formInfo.PokemonId)
                .ToListAsync();

            if (abilityIds.Count <= 1)
            {
                return new CommandResult { Message = "That Pokemon cannot have its ability changed!" };
            }

            var newIndex = (selectedPokemon.AbilityIndex + 1) % abilityIds.Count;
            var newAbilityId = abilityIds[newIndex].AbilityId;

            await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                .ExecuteUpdateAsync(p => p
                    .SetProperty(x => x.AbilityIndex, newIndex));

            var newAbility = await mongoDb.Abilities.Find(a => a.AbilityId == newAbilityId).FirstOrDefaultAsync();

            await db.Users.Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.MewCoins, x => x.MewCoins - price));

            return new CommandResult { Message = $"You have Successfully changed your Pokémon's ability to {newAbility.Identifier}" };
        }

        if (itemName == "ev-reset")
        {
            await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                .ExecuteUpdateAsync(p => p
                    .SetProperty(x => x.HpEv, 0)
                    .SetProperty(x => x.AttackEv, 0)
                    .SetProperty(x => x.DefenseEv, 0)
                    .SetProperty(x => x.SpecialAttackEv, 0)
                    .SetProperty(x => x.SpecialDefenseEv, 0)
                    .SetProperty(x => x.SpeedEv, 0));

            await db.Users.Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.MewCoins, x => x.MewCoins - price));

            return new CommandResult { Message = "You have successfully reset the Effort Values (EVs) of your selected Pokemon!" };
        }

        if (IsFormed(selectedPokemon.PokemonName))
        {
            return new CommandResult { Message = "You can not buy an item for a Form. Use `/deform` to de-form your Pokemon!" };
        }

        if (selectedPokemon.HeldItem.ToLower() != "none")
        {
            return new CommandResult { Message = "You already have an item equipped!" };
        }

        await db.Users.Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.MewCoins, x => x.MewCoins - price));

        await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
            .ExecuteUpdateAsync(p => p
                .SetProperty(x => x.HeldItem, itemName));

        var evolveResult = await pokemonService.TryEvolve(selectedPokemon.Id);

        return new CommandResult { Message = $"You have successfully bought the {itemName} for your {selectedPokemon.PokemonName}" };
    }

    public async Task<CommandResult> BuyDaycare(ulong userId, int amount)
    {
        if (amount < 0)
        {
            return new CommandResult { Message = "Yeah... negative numbers won't work here. Try again" };
        }

        var price = (ulong)(10000 * amount);
        await using var db = await dbContextProvider.GetContextAsync();
        var balance = await db.Users
            .Where(u => u.UserId == userId)
            .Select(u => u.MewCoins)
            .FirstOrDefaultAsyncEF();

        if (balance == null)
        {
            return new CommandResult { Message = "You have not started!\nStart with `/start` first." };
        }

        if (price > balance)
        {
            return new CommandResult
            {
                Message = $"You cannot afford that many daycare spaces! You need {price} credits, but you only have {balance}."
            };
        }

        await db.Users.Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.MewCoins, x => x.MewCoins - price)
                .SetProperty(x => x.DaycareLimit, x => x.DaycareLimit + amount));

        var plural = amount != 1 ? "s" : "";
        return new CommandResult { Message = $"You have successfully bought {amount} daycare space{plural}!" };
    }

    public async Task<CommandResult> BuyVitamins(ulong userId, string itemName, int amount)
    {
        amount = Math.Max(0, amount);
        itemName = itemName.Trim();
        var itemInfo = await mongoDb.Shop.Find(x => x.Item == itemName).FirstOrDefaultAsync();
        if (itemInfo == null)
        {
            return new CommandResult { Message = "That Item is not in the market" };
        }

        await using var db = await dbContextProvider.GetContextAsync();
        var totalPrice = (ulong)(amount * 100);
        var selectedId = await db.Users
            .Where(u => u.UserId == userId)
            .Select(u => u.Selected)
            .FirstOrDefaultAsyncEF();

        var selectedPokemon = await db.UserPokemon
            .FirstOrDefaultAsyncEF(p => p.Id == selectedId);

        if (selectedPokemon == null)
        {
            return new CommandResult { Message = "You don't have a pokemon selected!\nSelect one with `/select` first." };
        }

        var user = await db.Users.FirstOrDefaultAsyncEF(u => u.UserId == userId);
        if (user == null || user.MewCoins < totalPrice)
        {
            return new CommandResult { Message = $"You do not have {totalPrice} credits!" };
        }

        try
        {
            var evTotal = selectedPokemon.HpEv + selectedPokemon.AttackEv + selectedPokemon.DefenseEv +
                         selectedPokemon.SpecialAttackEv + selectedPokemon.SpecialDefenseEv + selectedPokemon.SpeedEv;

            if (evTotal + amount > 510)
            {
                return new CommandResult { Message = "Your Pokemon has maxed all 510 EVs or 252 EVs for that stat." };
            }

            await db.Users.Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.MewCoins, x => x.MewCoins - totalPrice));

            var updated = itemName switch
            {
                // Validate itemName and set the appropriate property update
                "calcium" => await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                    .ExecuteUpdateAsync(p => p.SetProperty(x => x.SpecialAttackEv, x => x.SpecialAttackEv + 10)),
                "carbos" => await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                    .ExecuteUpdateAsync(p => p.SetProperty(x => x.SpeedEv, x => x.SpeedEv + 10)),
                "hp-up" => await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                    .ExecuteUpdateAsync(p => p.SetProperty(x => x.HpEv, x => x.HpEv + 10)),
                "iron" => await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                    .ExecuteUpdateAsync(p => p.SetProperty(x => x.DefenseEv, x => x.DefenseEv + 10)),
                "protein" => await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                    .ExecuteUpdateAsync(p => p.SetProperty(x => x.AttackEv, x => x.AttackEv + 10)),
                "zinc" => await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                    .ExecuteUpdateAsync(p => p.SetProperty(x => x.SpecialDefenseEv, x => x.SpecialDefenseEv + 10)),
                _ => throw new ArgumentException("Invalid vitamin type")
            };


            return updated == 0 ? new CommandResult { Message = "Your Pokemon has maxed all 510 EVs or 252 EVs for that stat." } : new CommandResult { Message = $"You have successfully bought {amount} {itemName} for your {selectedPokemon.PokemonName}" };
        }
        catch
        {
            return new CommandResult { Message = "Your Pokemon has maxed all 510 EVs or 252 EVs for that stat." };
        }
    }

    public async Task<CommandResult> BuyCandy(ulong userId, int amount)
    {
        await using var db = await dbContextProvider.GetContextAsync();
        var selectedId = await db.Users
            .Where(u => u.UserId == userId)
            .Select(u => u.Selected)
            .FirstOrDefaultAsyncEF();

        if (selectedId == null)
        {
            return new CommandResult { Message = "You need to select a pokemon first!" };
        }

        var selectedPokemon = await db.UserPokemon.FirstOrDefaultAsyncEF(p => p.DittoId == selectedId);
        if (selectedPokemon == null)
        {
            return new CommandResult { Message = "Selected pokemon not found!" };
        }

        var credits = await db.Users
            .Where(u => u.UserId == userId)
            .Select(u => u.MewCoins)
            .FirstOrDefaultAsyncEF();

        var useAmount = Math.Max(0, Math.Min(100 - selectedPokemon.Level, amount));
        var buyAmount = useAmount == 0 ? 1 : useAmount;
        var price = (ulong)(buyAmount * 100);
        var candyStr = buyAmount == 1 ? "candy" : "candies";

        if (price > credits)
        {
            return new CommandResult
            {
                Message = $"You do not have {price} credits for {buyAmount} Rare {candyStr}",
                Ephemeral = true
            };
        }

        try
        {
            await db.UserPokemon.Where(p => p.DittoId == selectedId)
                .ExecuteUpdateAsync(p => p
                    .SetProperty(x => x.Level, x => x.Level + useAmount));

            await db.Users.Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.MewCoins, x => x.MewCoins - price));

            var newLevel = selectedPokemon.Level + useAmount;
            var evolveResult = await pokemonService.TryEvolve(selectedId.GetValueOrDefault(), overrideLvl100: true);

            return new CommandResult
            {
                Message = $"Your {selectedPokemon.PokemonName} has successfully leveled up to Level {newLevel}.",
                Ephemeral = true
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in Buy Candy - Poke: {Id} | Expected level - {Level}", selectedId, selectedPokemon.Level + useAmount);
            return new CommandResult
            {
                Message = "Sorry, I can't do that right now. Try again in a moment.",
                Ephemeral = true
            };
        }
    }

    public async Task<CommandResult> BuyChest(ulong userId, string chestType, string creditsOrRedeems)
{
    var ct = chestType.ToLower().Trim();
    var cor = creditsOrRedeems.ToLower();

    if (!new[] { "rare", "mythic", "legend" }.Contains(ct))
    {
        return new CommandResult
        {
            Message = $"`{ct}` is not a valid chest type! Choose one of Rare, Mythic, or Legend.",
            Ephemeral = true
        };
    }

    if (!new[] { "credits", "redeems" }.Contains(cor))
    {
        return new CommandResult
        {
            Message = "Specify either \"credits\" or \"redeems\"!",
            Ephemeral = true
        };
    }

    var prices = new Dictionary<string, Dictionary<string, int>>
    {
        ["credits"] = new()
        {
            ["rare"] = 300000,
            ["mythic"] = 600000,
            ["legend"] = 2000000
        },
        ["redeems"] = new()
        {
            ["rare"] = 7,
            ["mythic"] = 10,
            ["legend"] = 33
        }
    };

    var price = (ulong)prices[cor][ct];
    await using var db = await dbContextProvider.GetContextAsync();
    var user = await db.Users.FirstOrDefaultAsyncEF(u => u.UserId == userId);
    if (user == null)
    {
        return new CommandResult
        {
            Message = "You have not started!\nStart with `/start` first.",
            Ephemeral = true
        };
    }

    if (cor == "credits")
    {
        if (user.MewCoins < price)
        {
            return new CommandResult
            {
                Message = $"You do not have the {price} credits you need to buy a {ct} chest!",
                Ephemeral = true
            };
        }

        var chestStore = await db.ChestStore.FirstOrDefaultAsyncEF(c => c.UserId == userId);
        if (chestStore == null)
        {
            chestStore = new ChestStore
            {
                UserId = userId,
                Restock = ((DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 604800) + 1).ToString()
            };
            await db.ChestStore.AddAsync(chestStore);
        }

        var currentWeek = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 604800).ToString();
        if (long.Parse(chestStore.Restock) <= long.Parse(currentWeek))
        {
            chestStore.Rare = 0;
            chestStore.Mythic = 0;
            chestStore.Legend = 0;
            chestStore.Restock = (long.Parse(currentWeek) + 1).ToString();
        }

        const int maxChests = 5;
        var currentAmount = ct switch
        {
            "rare" => chestStore.Rare,
            "mythic" => chestStore.Mythic,
            "legend" => chestStore.Legend,
            _ => throw new ArgumentException("Invalid chest type")
        };

        if (currentAmount + 1 > maxChests)
        {
            return new CommandResult
            {
                Message = $"You can't buy more than {maxChests} per week using credits! You've already bought {currentAmount}.",
                Ephemeral = true
            };
        }

        // Update chest count
        switch (ct)
        {
            case "legend":
                chestStore.Legend++;
                break;
            case "mythic":
                chestStore.Mythic++;
                break;
            case "rare":
                chestStore.Rare++;
                break;
        }

        await db.SaveChangesAsync();
        await db.Users.Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.MewCoins, x => x.MewCoins - price));
    }
    else // redeems
    {
        if ((ulong)user.Redeems.GetValueOrDefault() < price)
        {
            return new CommandResult
            {
                Message = $"You do not have the {price} redeems you need to buy a {ct} chest!",
                Ephemeral = true
            };
        }

        await db.Users.Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => (ulong)x.Redeems.GetValueOrDefault(), x => (ulong)x.Redeems.GetValueOrDefault() - price));
    }

    var inventory = JsonSerializer.Deserialize<Dictionary<string, int>>(user.Inventory ?? "{}") ?? new();
    var item = $"{ct} chest";
    inventory[item] = inventory.GetValueOrDefault(item, 0) + 1;

    var serializedItems = JsonSerializer.Serialize(inventory);

    await db.Users.Where(u => u.UserId == userId)
        .ExecuteUpdateAsync(u => u
            .SetProperty(x => x.Inventory, serializedItems));

    return new CommandResult
    {
        Message = $"You have successfully bought a {ct} chest for {price} {cor}!\nYou can open it with `/open {ct}`."
    };
}

    public async Task<CommandResult> BuyRedeems(ulong userId, int? amount = null)
{
    if (amount.HasValue && amount.Value < 1)
    {
        return new CommandResult { Message = "Nice try..." };
    }

    await using var db = await dbContextProvider.GetContextAsync();
    var user = await db.Users.FirstOrDefaultAsyncEF(u => u.UserId == userId);
    if (user == null)
    {
        return new CommandResult
        {
            Message = "You have not started!\nStart with `/start` first.",
            Ephemeral = true
        };
    }

    var redeemStore = await db.RedeemStore.FirstOrDefaultAsyncEF(r => r.UserId == userId);
    if (redeemStore == null)
    {
        redeemStore = new RedeemStore
        {
            UserId = userId,
            Restock = ((DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 604800) + 1).ToString()
        };
        await db.RedeemStore.AddAsync(redeemStore);
        await db.SaveChangesAsync();
    }

    const int maxRedeems = 100;
    const int restock_time = 604800;

    var currentWeek = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() / restock_time).ToString();
    if (long.Parse(redeemStore.Restock) <= long.Parse(currentWeek))
    {
        redeemStore.Bought = 0;
        redeemStore.Restock = (long.Parse(currentWeek) + 1).ToString();
        await db.SaveChangesAsync();
    }

    if (!amount.HasValue)
    {
        var embed = new EmbedBuilder();
        if (redeemStore.Restock != "0")
        {
            var desc = $"You have bought {redeemStore.Bought} redeems this week.\n";
            if (redeemStore.Bought >= maxRedeems)
            {
                desc += "You cannot buy any more this week.";
            }
            else
            {
                desc += "Buy more using `/buy redeems <amount>`!";
            }

            embed.WithTitle("Buy redeems")
                .WithDescription(desc)
                .WithColor(new Color(255, 182, 193))
                .WithFooter("Redeems restock every Wednesday at 8pm ET.");
        }
        else
        {
            embed.WithTitle("Buy redeems")
                .WithDescription("You haven't bought any redeems yet! Use `/buy redeems <amount>`!")
                .WithColor(new Color(255, 182, 193));
        }

        return new CommandResult { Embed = embed.Build(), Ephemeral = true };
    }

    if (redeemStore.Bought + amount.Value > maxRedeems)
    {
        return new CommandResult
        {
            Message = $"You can't buy more than {maxRedeems} per week! You've already bought {redeemStore.Bought}.",
            Ephemeral = true
        };
    }

    const int creditsPerRedeem = 60000;
    var price = (ulong)(amount.Value * creditsPerRedeem);

    if (user.MewCoins < price)
    {
        return new CommandResult
        {
            Message = $"You do not have the {price} credits to buy those redeems!"
        };
    }

    await db.Users.Where(u => u.UserId == userId)
        .ExecuteUpdateAsync(u => u
            .SetProperty(x => x.Redeems, x => x.Redeems + amount.Value)
            .SetProperty(x => x.MewCoins, x => x.MewCoins - price));

    redeemStore.Bought += amount.Value;
    if (redeemStore.Restock == "0")
    {
        redeemStore.Restock = ((DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 604800) + 1).ToString();
    }
    await db.SaveChangesAsync();

    return new CommandResult
    {
        Message = $"You have successfully bought {amount.Value} redeems for {price} credits!"
    };
}

    public record CommandResult
    {
        public string Message { get; init; }
        public Embed Embed { get; init; }
        public bool Ephemeral { get; init; }
        public bool Success => !string.IsNullOrEmpty(Message) || Embed != null;
    }
}