﻿using Microsoft.EntityFrameworkCore;
using Ditto.Database.Models.PostgreSQL.Pokemon;
using Ditto.Database.Models.PostgreSQL.Ai;
using Ditto.Database.Models.PostgreSQL.Bot;
using Ditto.Database.Models.PostgreSQL.Art;
using Ditto.Database.Models.PostgreSQL.Game;

namespace Ditto.Database;

public class DittoContext : DbContext
{
    public DittoContext(DbContextOptions<DittoContext> options) : base(options) { }

    #region Pokemon Models
    /// <summary>
    /// Represents player achievements in the game
    /// </summary>
    public DbSet<Achievement> Achievements { get; set; }

    /// <summary>
    /// Represents deceased radiant Pokémon
    /// </summary>
    public DbSet<DeadRadiant> DeadRadiants { get; set; }

    /// <summary>
    /// Represents deceased regular Pokémon
    /// </summary>
    public DbSet<DeadPokemon> DeadPokemon { get; set; }

    /// <summary>
    /// Represents Pokémon eggs in the game
    /// </summary>
    public DbSet<Eggs> Eggs { get; set; }

    /// <summary>
    /// Represents the egg hatchery system
    /// </summary>
    public DbSet<EggHatchery> EggHatcheries { get; set; }

    public DbSet<DeadPokemon> DeadPokemons { get; set; }

    /// <summary>
    /// Represents Pokémon in the hatchery
    /// </summary>
    public DbSet<HatcheryPokemon> HatcheryPokemon { get; set; }

    /// <summary>
    /// Represents honey items used to attract Pokémon
    /// </summary>
    public DbSet<Honey> Honey { get; set; }

    /// <summary>
    /// Represents mother Pokémon used for breeding
    /// </summary>
    public DbSet<Mother> Mothers { get; set; }

    /// <summary>
    /// Represents individual Pokémon parties
    /// </summary>
    public DbSet<Party> Parties { get; set; }

    /// <summary>
    /// Represents Pokémon in the game
    /// </summary>
    public DbSet<Pokemon> UserPokemon { get; set; }

    /// <summary>
    /// Represents Pokémon statistics
    /// </summary>
    public DbSet<PokemonStats> PokemonStats { get; set; }

    /// <summary>
    /// Represents total Pokémon counts and statistics
    /// </summary>
    public DbSet<PokemonTotal> PokemonTotals { get; set; }
    #endregion

    #region AI Models
    /// <summary>
    /// Represents AI image generations
    /// </summary>
    public DbSet<AiGeneration> AiGenerations { get; set; }

    /// <summary>
    /// Represents themes for AI image generation
    /// </summary>
    public DbSet<AiTheme> AiThemes { get; set; }
    #endregion

    #region Bot Models
    /// <summary>
    /// Represents bot announcements
    /// </summary>
    public DbSet<Announce> Announcements { get; set; }

    /// <summary>
    /// Represents banned users from the bot
    /// </summary>
    public DbSet<BotBan> BotBans { get; set; }

    /// <summary>
    /// Represents calendar events
    /// </summary>
    public DbSet<Cal> Calendar { get; set; }

    /// <summary>
    /// Represents community information
    /// </summary>
    public DbSet<Community> Communities { get; set; }

    /// <summary>
    /// Represents disabled channels
    /// </summary>
    public DbSet<DisabledChannel> DisabledChannels { get; set; }

    /// <summary>
    /// Represents Ditto-specific donations
    /// </summary>
    public DbSet<DittoDonation> DittoDonations { get; set; }

    /// <summary>
    /// Represents general donations
    /// </summary>
    public DbSet<Donation> Donations { get; set; }

    /// <summary>
    /// Represents stored messages
    /// </summary>
    public DbSet<Message> Messages { get; set; }

    /// <summary>
    /// Represents new user registrations
    /// </summary>
    public DbSet<NewUser> NewUsers { get; set; }

    /// <summary>
    /// Represents server configurations
    /// </summary>
    public DbSet<Server> Servers { get; set; }

    /// <summary>
    /// Represents sky-related logs
    /// </summary>
    public DbSet<SkyLog> SkyLogs { get; set; }

    /// <summary>
    /// Represents authentication tokens
    /// </summary>
    public DbSet<Token> Tokens { get; set; }

    /// <summary>
    /// Represents bot updates
    /// </summary>
    public DbSet<Update> Updates { get; set; }

    /// <summary>
    /// Represents bot users
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Represents voucher requests
    /// </summary>
    public DbSet<VoucherRequest> VoucherRequests { get; set; }

    /// <summary>
    /// Represents active users
    /// </summary>
    public DbSet<ActiveUser> ActiveUsers { get; set; }

    /// <summary>
    /// Represents inactive users
    /// </summary>
    public DbSet<InactiveUser> InactiveUsers { get; set; }
    #endregion

    #region Art Models
    /// <summary>
    /// Represents artists
    /// </summary>
    public DbSet<Artist> Artists { get; set; }

    /// <summary>
    /// Represents artist consent records
    /// </summary>
    public DbSet<ArtistConsent> ArtistConsents { get; set; }
    #endregion

    #region Game Models
    /// <summary>
    /// Represents the chest store
    /// </summary>
    public DbSet<ChestStore> ChestStore { get; set; }

    /// <summary>
    /// Represents current events
    /// </summary>
    public DbSet<CurrentEvent> CurrentEvents { get; set; }

    /// <summary>
    /// Represents Ditto Bitty items
    /// </summary>
    public DbSet<DittoBitty> DittoBitties { get; set; }

    /// <summary>
    /// Represents gifts
    /// </summary>
    public DbSet<Gift> Gifts { get; set; }

    /// <summary>
    /// Represents gyms
    /// </summary>
    public DbSet<Gym> Gyms { get; set; }

    /// <summary>
    /// Represents gym activity logs
    /// </summary>
    public DbSet<GymLog> GymLogs { get; set; }

    /// <summary>
    /// Represents Halloween event data
    /// </summary>
    public DbSet<Halloween> Halloween { get; set; }

    /// <summary>
    /// Represents leveling system data
    /// </summary>
    public DbSet<LevelingData> LevelingData { get; set; }

    /// <summary>
    /// Represents the marketplace
    /// </summary>
    public DbSet<Market> Market { get; set; }

    /// <summary>
    /// Represents the Patreon store
    /// </summary>
    public DbSet<PatreonStore> PatreonStore { get; set; }

    /// <summary>
    /// Represents the redeem store
    /// </summary>
    public DbSet<RedeemStore> RedeemStore { get; set; }

    /// <summary>
    /// Represents tournament teams
    /// </summary>
    public DbSet<TournamentTeam> TournamentTeams { get; set; }

    /// <summary>
    /// Represents trade logs
    /// </summary>
    public DbSet<TradeLog> TradeLogs { get; set; }

    /// <summary>
    /// Represents user Ditto Bitty collections
    /// </summary>
    public DbSet<UserDittoBitty> UserDittoBitties { get; set; }
    #endregion
}