using Microsoft.EntityFrameworkCore;
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
    public DbSet<Egg> Eggs { get; set; }

    /// <summary>
    /// Represents the egg hatchery system
    /// </summary>
    public DbSet<EggHatchery> EggHatcheries { get; set; }

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Pokemon Models Configuration
        modelBuilder.Entity<Pokemon>()
            .HasKey(p => p.Id);
        modelBuilder.Entity<Pokemon>()
            .HasIndex(p => p.MarketEnlist);

        modelBuilder.Entity<DeadPokemon>()
            .HasKey(p => p.Id);
        modelBuilder.Entity<DeadPokemon>()
            .HasIndex(p => p.MarketEnlist);

        modelBuilder.Entity<DeadRadiant>()
            .HasKey(p => p.Pokemon);
        modelBuilder.Entity<DeadRadiant>()
            .HasIndex(p => p.Id)
            .IsUnique();

        modelBuilder.Entity<PokemonStats>()
            .HasKey(p => p.Pokemon);

        modelBuilder.Entity<Achievement>()
            .HasKey(a => a.UserId);

        modelBuilder.Entity<Egg>()
            .HasKey(e => e.UserId);

        modelBuilder.Entity<EggHatchery>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<EggHatchery>()
            .HasIndex(e => e.UserId);
        modelBuilder.Entity<EggHatchery>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId);
        modelBuilder.Entity<EggHatchery>()
            .HasOne<Pokemon>()
            .WithMany()
            .HasForeignKey("1");
        modelBuilder.Entity<EggHatchery>()
            .HasOne<Pokemon>()
            .WithMany()
            .HasForeignKey("2");
        modelBuilder.Entity<EggHatchery>()
            .HasOne<Pokemon>()
            .WithMany()
            .HasForeignKey("3");
        modelBuilder.Entity<EggHatchery>()
            .HasOne<Pokemon>()
            .WithMany()
            .HasForeignKey("4");
        modelBuilder.Entity<EggHatchery>()
            .HasOne<Pokemon>()
            .WithMany()
            .HasForeignKey("5");
        modelBuilder.Entity<EggHatchery>()
            .HasOne<Pokemon>()
            .WithMany()
            .HasForeignKey("6");
        modelBuilder.Entity<EggHatchery>()
            .HasOne<Pokemon>()
            .WithMany()
            .HasForeignKey("7");
        modelBuilder.Entity<EggHatchery>()
            .HasOne<Pokemon>()
            .WithMany()
            .HasForeignKey("8");
        modelBuilder.Entity<EggHatchery>()
            .HasOne<Pokemon>()
            .WithMany()
            .HasForeignKey("9");
        modelBuilder.Entity<EggHatchery>()
            .HasOne<Pokemon>()
            .WithMany()
            .HasForeignKey("10");

        modelBuilder.Entity<Honey>()
            .HasKey(h => h.Id);

        modelBuilder.Entity<Party>()
            .HasKey(p => p.PartyId);

        // AI Models Configuration
        modelBuilder.Entity<AiGeneration>()
            .HasKey(a => new { a.Id, a.Theme });

        modelBuilder.Entity<AiTheme>()
            .HasKey(a => new { a.Id, a.Name });

        // Bot Models Configuration
        modelBuilder.Entity<Announce>()
            .HasKey(a => a.Id);

        modelBuilder.Entity<Message>()
            .HasKey(m => m.MessageId);

        modelBuilder.Entity<NewUser>()
            .HasKey(u => u.UserId);

        modelBuilder.Entity<Server>()
            .HasKey(s => s.ServerId);

        modelBuilder.Entity<SkyLog>()
            .HasKey(s => s.Id);

        modelBuilder.Entity<Token>()
            .HasIndex(t => t.TokenValue)
            .IsUnique();
        modelBuilder.Entity<Token>()
            .HasIndex(t => t.UserId)
            .IsUnique();

        modelBuilder.Entity<Update>()
            .HasKey(u => u.Id);

        modelBuilder.Entity<User>()
            .HasKey(u => u.UserId);
        modelBuilder.Entity<User>()
            .HasIndex(u => u.UserId)
            .IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.TrainerNickname)
            .IsUnique();

        modelBuilder.Entity<VoucherRequest>()
            .HasKey(v => v.MessageId);

        // Art Models Configuration
        modelBuilder.Entity<Artist>()
            .HasKey(a => a.Id);

        modelBuilder.Entity<ArtistConsent>()
            .HasKey(a => a.Artist);

        // Game Models Configuration
        modelBuilder.Entity<ChestStore>()
            .HasKey(c => c.UserId);
        modelBuilder.Entity<ChestStore>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId);

        modelBuilder.Entity<CurrentEvent>()
            .HasKey(c => c.UserId);

        modelBuilder.Entity<DittoBitty>()
            .HasKey(d => d.Id);

        modelBuilder.Entity<Gift>()
            .HasKey(g => g.GiftId);

        modelBuilder.Entity<Gym>()
            .HasKey(g => g.UserId);

        modelBuilder.Entity<GymLog>()
            .HasKey(g => g.Id);

        modelBuilder.Entity<Halloween>()
            .HasKey(h => h.UserId);

        modelBuilder.Entity<Market>()
            .HasKey(m => m.Id);

        modelBuilder.Entity<BotBan>()
            .HasNoKey();

        modelBuilder.Entity<PatreonStore>()
            .HasKey(p => p.UserId);

        modelBuilder.Entity<RedeemStore>()
            .HasKey(r => r.UserId);
        modelBuilder.Entity<RedeemStore>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId);

        modelBuilder.Entity<TournamentTeam>()
            .HasKey(t => t.UserId);

        modelBuilder.Entity<TradeLog>()
            .HasKey(t => t.TradeId);

        modelBuilder.Entity<UserDittoBitty>()
            .HasKey(u => u.Id);
        modelBuilder.Entity<UserDittoBitty>()
            .HasOne<DittoBitty>()
            .WithMany()
            .HasForeignKey(u => u.DittoBittyId);
    }
}