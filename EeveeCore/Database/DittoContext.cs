﻿using EeveeCore.Database.Models.PostgreSQL.Art;
using EeveeCore.Database.Models.PostgreSQL.Bot;
using EeveeCore.Database.Models.PostgreSQL.Game;
using EeveeCore.Database.Models.PostgreSQL.Pokemon;
using Microsoft.EntityFrameworkCore;

namespace EeveeCore.Database;

/// <summary>
///     Represents the main database context for the EeveeCore Pokémon bot system.
///     This class provides access to all database entities through DbSet properties,
///     organized into logical categories for Pokémon, Bot, Art, and Game models.
/// </summary>
public class EeveeCoreContext(DbContextOptions<EeveeCoreContext> options) : DbContext(options)
{
    /// <summary>
    ///     Configures the entity relationships and constraints.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to construct the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserPokemonOwnership>()
            .HasIndex(u => new { u.UserId, u.Position })
            .IsUnique();

        modelBuilder.Entity<UserPokemonOwnership>()
            .HasIndex(u => u.UserId);

        modelBuilder.Entity<UserPokemonOwnership>()
            .HasIndex(u => u.PokemonId);

        modelBuilder.Entity<UserPokemonOwnership>()
            .Property(o => o.Id)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<UserPokemonOwnership>()
            .HasOne(upo => upo.User)
            .WithMany()
            .HasForeignKey(upo => upo.UserId)
            .HasPrincipalKey(u => u.UserId);

        modelBuilder.Entity<InvalidPokemonReference>()
            .HasNoKey()
            .HasIndex(i => i.UserId);
    }

    #region Pokemon Models

    /// <summary>
    ///     Represents player achievements in the game
    /// </summary>
    public DbSet<Achievement> Achievements { get; set; }

    /// <summary>
    ///     Represents deceased radiant Pokémon
    /// </summary>
    public DbSet<DeadRadiant> DeadRadiants { get; set; }

    /// <summary>
    ///     Represents deceased regular Pokémon
    /// </summary>
    public DbSet<DeadPokemon> DeadPokemon { get; set; }

    /// <summary>
    ///     Represents Pokémon eggs in the game
    /// </summary>
    public DbSet<Eggs> Eggs { get; set; }

    /// <summary>
    ///     Represents the egg hatchery system
    /// </summary>
    public DbSet<EggHatchery> EggHatcheries { get; set; }

    /// <summary>
    ///     Represents archived Pokémon data
    /// </summary>
    public DbSet<DeadPokemon> DeadPokemons { get; set; }

    /// <summary>
    ///     Represents Pokémon in the hatchery
    /// </summary>
    public DbSet<HatcheryPokemon> HatcheryPokemon { get; set; }

    /// <summary>
    ///     Represents honey items used to attract Pokémon
    /// </summary>
    public DbSet<Honey> Honey { get; set; }

    /// <summary>
    ///     Represents mother Pokémon used for breeding
    /// </summary>
    public DbSet<Mother> Mothers { get; set; }

    /// <summary>
    ///     Represents individual Pokémon parties
    /// </summary>
    public DbSet<Party> Parties { get; set; }

    /// <summary>
    ///     Represents Pokémon in the game
    /// </summary>
    public DbSet<Pokemon> UserPokemon { get; set; }

    /// <summary>
    ///     Represents Pokémon statistics
    /// </summary>
    public DbSet<PokemonStats> PokemonStats { get; set; }

    /// <summary>
    ///     Represents total Pokémon counts and statistics
    /// </summary>
    public DbSet<PokemonTotal> PokemonTotals { get; set; }

    /// <summary>
    ///     Gets or sets the user-Pokémon ownership relationships.
    /// </summary>
    public DbSet<UserPokemonOwnership> UserPokemonOwnerships { get; set; }

    /// <summary>
    ///     Gets or sets the invalid Pokémon references that were detected during migration.
    /// </summary>
    public DbSet<InvalidPokemonReference> InvalidPokemonReferences { get; set; }

    #endregion


    #region Bot Models

    /// <summary>
    ///     Represents bot announcements
    /// </summary>
    public DbSet<Announce> Announcements { get; set; }

    /// <summary>
    ///     Represents banned users from the bot
    /// </summary>
    public DbSet<BotBan> BotBans { get; set; }

    /// <summary>
    ///     Represents calendar events
    /// </summary>
    public DbSet<Cal> Calendar { get; set; }

    /// <summary>
    ///     Represents community information
    /// </summary>
    public DbSet<Community> Communities { get; set; }

    /// <summary>
    ///     Represents disabled channels
    /// </summary>
    public DbSet<DisabledChannel> DisabledChannels { get; set; }

    /// <summary>
    ///     Represents EeveeCore-specific donations
    /// </summary>
    public DbSet<EeveeCoreDonation> EeveeCoreDonations { get; set; }

    /// <summary>
    ///     Represents general donations
    /// </summary>
    public DbSet<Donation> Donations { get; set; }

    /// <summary>
    ///     Represents new user registrations
    /// </summary>
    public DbSet<NewUser> NewUsers { get; set; }

    /// <summary>
    ///     Represents server configurations
    /// </summary>
    public DbSet<Server> Servers { get; set; }

    /// <summary>
    ///     Represents sky-related logs
    /// </summary>
    public DbSet<SkyLog> SkyLogs { get; set; }

    /// <summary>
    ///     Represents authentication tokens
    /// </summary>
    public DbSet<Token> Tokens { get; set; }

    /// <summary>
    ///     Represents bot updates
    /// </summary>
    public DbSet<Update> Updates { get; set; }

    /// <summary>
    ///     Represents bot users
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    ///     Represents voucher requests
    /// </summary>
    public DbSet<VoucherRequest> VoucherRequests { get; set; }

    /// <summary>
    ///     Represents active users
    /// </summary>
    public DbSet<ActiveUser> ActiveUsers { get; set; }

    /// <summary>
    ///     Represents inactive users
    /// </summary>
    public DbSet<InactiveUser> InactiveUsers { get; set; }

    #endregion

    #region Art Models

    /// <summary>
    ///     Represents artists
    /// </summary>
    public DbSet<Artist> Artists { get; set; }

    /// <summary>
    ///     Represents artist consent records
    /// </summary>
    public DbSet<ArtistConsent> ArtistConsents { get; set; }

    #endregion

    #region Game Models

    /// <summary>
    ///     Represents the chest store
    /// </summary>
    public DbSet<ChestStore> ChestStore { get; set; }

    /// <summary>
    ///     Represents current events
    /// </summary>
    public DbSet<CurrentEvent> CurrentEvents { get; set; }

    /// <summary>
    ///     Represents gifts
    /// </summary>
    public DbSet<Gift> Gifts { get; set; }

    /// <summary>
    ///     Represents gyms
    /// </summary>
    public DbSet<Gym> Gyms { get; set; }

    /// <summary>
    ///     Represents gym activity logs
    /// </summary>
    public DbSet<GymLog> GymLogs { get; set; }

    /// <summary>
    ///     Represents Halloween event data
    /// </summary>
    public DbSet<Halloween> Halloween { get; set; }

    /// <summary>
    ///     Represents leveling system data
    /// </summary>
    public DbSet<LevelingData> LevelingData { get; set; }

    /// <summary>
    ///     Represents the marketplace
    /// </summary>
    public DbSet<Market> Market { get; set; }

    /// <summary>
    ///     Represents the Patreon store
    /// </summary>
    public DbSet<PatreonStore> PatreonStore { get; set; }

    /// <summary>
    ///     Represents the redeem store
    /// </summary>
    public DbSet<RedeemStore> RedeemStore { get; set; }

    /// <summary>
    ///     Represents tournament teams
    /// </summary>
    public DbSet<TournamentTeam> TournamentTeams { get; set; }

    /// <summary>
    ///     Represents trade logs
    /// </summary>
    public DbSet<TradeLog> TradeLogs { get; set; }

    #endregion
}