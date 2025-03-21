using EeveeCore.Database.Models.Mongo.Discord;
using EeveeCore.Database.Models.Mongo.Game;
using EeveeCore.Database.Models.Mongo.Pokemon;
using MongoDB.Driver;
using Pokemon_Type = EeveeCore.Database.Models.Mongo.Pokemon.Type;
using Type = EeveeCore.Database.Models.Mongo.Pokemon.Type;

namespace EeveeCore.Services.Impl;

public class MongoService(IMongoClient client) : IMongoService
{
    private readonly IMongoDatabase _database = client.GetDatabase("pokemon");

    // Pokemon Data
    public IMongoCollection<Ability> Abilities => _database.GetCollection<Ability>("abilities");
    public IMongoCollection<Form> Forms => _database.GetCollection<Form>("forms");
    public IMongoCollection<Move> Moves => _database.GetCollection<Move>("moves");
    public IMongoCollection<Nature> Natures => _database.GetCollection<Nature>("natures");
    public IMongoCollection<PokemonFile> PFile => _database.GetCollection<PokemonFile>("pfile");
    public IMongoCollection<PokemonAbility> PokeAbilities => _database.GetCollection<PokemonAbility>("poke_abilities");
    public IMongoCollection<PokemonMoves> PokemonMoves => _database.GetCollection<PokemonMoves>("pokemon_moves");
    public IMongoCollection<PokemonStats> PokemonStats => _database.GetCollection<PokemonStats>("pokemon_stats");
    public IMongoCollection<PokemonTypes> PokemonTypes => _database.GetCollection<PokemonTypes>("ptypes");
    public IMongoCollection<StatType> StatTypes => _database.GetCollection<StatType>("stat_types");
    public IMongoCollection<Pokemon_Type> Types => _database.GetCollection<Pokemon_Type>("types");

    public IMongoCollection<TypeEffectiveness> TypeEffectiveness =>
        _database.GetCollection<TypeEffectiveness>("type_effectiveness");

    // Game Data
    public IMongoCollection<Booster> Boosters => _database.GetCollection<Booster>("boosters");

    public IMongoCollection<CurrentRadiant> CurrentRadiants =>
        _database.GetCollection<CurrentRadiant>("current_radiants");

    public IMongoCollection<EggGroup> EggGroups => _database.GetCollection<EggGroup>("egg_groups");
    public IMongoCollection<EggGroupInfo> EggGroupsInfo => _database.GetCollection<EggGroupInfo>("egg_groups_info");
    public IMongoCollection<Evolution> Evolution => _database.GetCollection<Evolution>("evofile");
    public IMongoCollection<Filter> Filters => _database.GetCollection<Filter>("filters");
    public IMongoCollection<Gift> Gifts => _database.GetCollection<Gift>("gifts");
    public IMongoCollection<Gym> Gyms => _database.GetCollection<Gym>("gyms");
    public IMongoCollection<Item> Items => _database.GetCollection<Item>("items");
    public IMongoCollection<Levels> Levels => _database.GetCollection<Levels>("levels");
    public IMongoCollection<Milestone> Milestones => _database.GetCollection<Milestone>("milestones");
    public IMongoCollection<Mission> Missions => _database.GetCollection<Mission>("missions2");
    public IMongoCollection<Month> Months => _database.GetCollection<Month>("month");

    public IMongoCollection<RadiantPlaceholder> RadiantPlaceholders =>
        _database.GetCollection<RadiantPlaceholder>("radiant_placeholder_pokes");

    public IMongoCollection<ShopItem> Shop => _database.GetCollection<ShopItem>("shop");
    public IMongoCollection<UserProgress> UserProgress => _database.GetCollection<UserProgress>("user_progress");

    // Discord Data
    public IMongoCollection<Guild> Guilds => _database.GetCollection<Guild>("guilds");
    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
}