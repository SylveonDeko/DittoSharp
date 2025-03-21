using EeveeCore.Database.Models.Mongo.Discord;
using EeveeCore.Database.Models.Mongo.Game;
using EeveeCore.Database.Models.Mongo.Pokemon;
using MongoDB.Driver;
using Pokemon_Type = EeveeCore.Database.Models.Mongo.Pokemon.Type;
using Type = EeveeCore.Database.Models.Mongo.Pokemon.Type;

namespace EeveeCore.Services.Impl;

public interface IMongoService
{
    // Pokemon Data
    IMongoCollection<Ability> Abilities { get; }
    IMongoCollection<Form> Forms { get; }
    IMongoCollection<Move> Moves { get; }
    IMongoCollection<Nature> Natures { get; }
    IMongoCollection<PokemonFile> PFile { get; }
    IMongoCollection<PokemonAbility> PokeAbilities { get; }
    IMongoCollection<PokemonMoves> PokemonMoves { get; }
    IMongoCollection<PokemonStats> PokemonStats { get; }
    IMongoCollection<PokemonTypes> PokemonTypes { get; }
    IMongoCollection<StatType> StatTypes { get; }
    IMongoCollection<Pokemon_Type> Types { get; }
    IMongoCollection<TypeEffectiveness> TypeEffectiveness { get; }

    // Game Data
    IMongoCollection<Booster> Boosters { get; }
    IMongoCollection<CurrentRadiant> CurrentRadiants { get; }
    IMongoCollection<EggGroup> EggGroups { get; }
    IMongoCollection<EggGroupInfo> EggGroupsInfo { get; }
    IMongoCollection<Evolution> Evolution { get; }
    IMongoCollection<Filter> Filters { get; }
    IMongoCollection<Gift> Gifts { get; }
    IMongoCollection<Gym> Gyms { get; }
    IMongoCollection<Item> Items { get; }
    IMongoCollection<Levels> Levels { get; }
    IMongoCollection<Milestone> Milestones { get; }
    IMongoCollection<Mission> Missions { get; }
    IMongoCollection<Month> Months { get; }
    IMongoCollection<RadiantPlaceholder> RadiantPlaceholders { get; }
    IMongoCollection<ShopItem> Shop { get; }
    IMongoCollection<UserProgress> UserProgress { get; }

    // Discord Data
    IMongoCollection<Guild> Guilds { get; }
    IMongoCollection<User> Users { get; }
}