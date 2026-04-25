# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DittoSharp is a Discord bot project focused on Pokemon collection and interaction gameplay. The main component is **EeveeCore**, a comprehensive .NET 9.0 Discord bot that integrates with multiple databases and provides both Discord slash commands and a web API.

## Technology Stack

- **.NET 9.0** with ASP.NET Core for web API
- **Discord.NET 3.18.0-beta.3** for Discord integration (sharded client)
- **LinqToDB 6.0.0-preview.4** for PostgreSQL database operations
- **MongoDB 3.4.0** for static game data storage
- **Redis (StackExchange.Redis 2.8.41)** for high-performance caching
- **Serilog** for structured logging
- **JWT authentication** for web API access
- **DbUp-PostgreSQL** for database migrations

## Core Architecture

### Database Design (Multi-Database)

The bot uses a three-tier database architecture:

1. **PostgreSQL** (Primary) - via LinqToDB `DittoDataConnection`
   - User Pokemon collections and ownership
   - Game state, trades, and user profiles  
   - Fraud detection and analytics
   - All transactional data

2. **MongoDB** (Static Data) - via `IMongoService`
   - Pokemon reference data (moves, abilities, types)
   - Guild configurations and settings
   - Items, missions, and game mechanics

3. **Redis** (Caching) - via `RedisCache`
   - High-performance JSON serialization caching
   - Guild settings with TTL management
   - Session state for complex operations

### Discord Integration

- **Sharded Client**: `DiscordShardedClient` with configurable sharding
- **Slash Commands**: Primary interaction method via `InteractionService`
- **Command Queuing**: Per-channel queues prevent race conditions
- **Global Cooldown**: 750ms cooldown system via `CommandHandler`
- **Module System**: Organized command modules with service injection

### Module Behavior System

The bot implements a sophisticated module behavior system with lifecycle hooks:

- `IReadyExecutor` - Execute when bot is ready
- `IEarlyBlocker` - Early command blocking logic  
- `ILateBlocker` - Late command blocking logic
- `IInputTransformer` - Transform user input
- `ILateExecutor` - Execute after command completion

### Web API Architecture

- **Dual Authentication**: API key and JWT Bearer token schemes
- **Authorization Policies**: AdminPolicy, BotOwnerPolicy, JwtPolicy, ApiKeyPolicy
- **CORS Configuration**: Configured for development servers
- **Swagger Integration**: Available in development mode

## Development Commands

### Building and Running

```bash
# Build the solution
dotnet build DittoSharp.sln

# Run in development mode (uses DebugGuildId for slash commands)
dotnet run --project EeveeCore/EeveeCore.csproj --configuration Debug

# Run in production mode (registers global slash commands)
dotnet run --project EeveeCore/EeveeCore.csproj --configuration Release

# Restore packages
dotnet restore DittoSharp.sln
```

### Database Operations

Database migrations are handled automatically on startup via `DatabaseMigrator` using DbUp:

```bash
# Migrations are embedded resources in Database/Migrations/*.sql
# They run automatically when the application starts

# Connection configuration is in config.json:
# "PostgresConnectionString": "Host=localhost;Port=5432;Database=mewbot;"
```

### Configuration

The bot requires a `config.json` file with the following structure:

```json
{
  "Token": "your_discord_bot_token",
  "OwnerIds": [123456789],
  "DebugGuildId": "guild_id_for_development",
  "PostgresConnectionString": "connection_string",
  "MongoConnectionString": "mongodb://localhost:27017",
  "RedisConnectionString": "localhost:6379",
  "IsApiEnabled": true,
  "ApiPort": 5045,
  "JwtSecret": "secure_jwt_secret",
  "DiscordClientId": "discord_app_id",
  "DiscordClientSecret": "discord_app_secret"
}
```

## Key Architectural Patterns

### LinqToDB Usage

The project uses LinqToDB instead of Entity Framework for PostgreSQL operations:

```csharp
// Connection provider pattern
using var db = await _dbProvider.GetConnectionAsync();

// Table access via DittoDataConnection
var pokemon = await db.UserPokemon
    .Where(p => p.UserId == userId)
    .ToListAsync();

// Connection is configured to CloseAfterUse = true
```

### Discord.NET Patterns

```csharp
// Slash command modules inherit from EeveeCoreSlashModuleBase
[Group("pokemon", "Pokemon related commands")]
public class PokemonSlashCommands : EeveeCoreSlashModuleBase<PokemonService>

// Sharded client configuration with full intents
var client = new DiscordShardedClient(new DiscordSocketConfig
{
    MessageCacheSize = 15,
    LogLevel = LogSeverity.Debug,
    GatewayIntents = GatewayIntents.All,
    DefaultRetryMode = RetryMode.RetryRatelimit
});
```

### Service Injection

Services are registered via Scrutor for automatic discovery:

```csharp
services.Scan(scan => scan.FromAssemblyOf<IReadyExecutor>()
    .AddClasses(classes => classes.AssignableToAny(
        typeof(INService),
        typeof(IEarlyBehavior),
        typeof(ILateBlocker)))
    .AsSelfWithInterfaces()
    .WithSingletonLifetime());
```

### Command Handler Architecture

- **Per-Channel Queuing**: `ConcurrentDictionary<ulong, ConcurrentQueue<IUserMessage>>`
- **Command Lock**: `ConcurrentDictionary<ulong, bool>` prevents race conditions
- **Global Cooldown**: 750ms via `_usersOnShortCooldown` ConcurrentHashSet
- **Error Isolation**: Per-command try-catch prevents cascading failures

## Module Organization

The Discord commands are organized into logical modules under `/Modules/`:

- **Pokemon/**: Core Pokemon collection management
- **Spawn/**: Pokemon spawning system  
- **Start/**: User onboarding and starter selection
- **Breeding/**: Pokemon breeding mechanics
- **Duels/**: Pokemon battle system
- **Items/**: Inventory and item management
- **Market/**: Trading marketplace
- **Trade/**: Direct trading + fraud detection
- **Parties/**: Pokemon party management
- **Fishing/**: Fishing game mechanics
- **Missions/**: Mission/quest system

Each module follows the pattern:
- `{Module}SlashCommands.cs` - Discord slash command definitions
- `Services/{Module}Service.cs` - Business logic implementation
- `Components/` - Discord UI components (buttons, dropdowns)

## Database Schema Highlights

### LinqToDB Models (PostgreSQL)

Key entity relationships in `/Database/Linq/Models/`:

- **Pokemon/**: `Pokemon`, `UserPokemonOwnership`, `Party`, `Eggs`
- **Bot/**: `User`, `Server`, `ActiveUser`, `UserFilterGroup`  
- **Game/**: `TradeLog`, `Market`, `ActiveSpawn`, `TradeFraudDetection`
- **Art/**: `Artist`, `ArtistConsent`

### MongoDB Collections

Handled via `IMongoService` for static/reference data:

- Pokemon moves, abilities, types, and forms
- Guild settings and configurations
- Items, shop data, and mission definitions

## API Development

### Controller Pattern

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Jwt")]
public class UserController : ControllerBase
{
    // LinqToDB injection pattern
    private readonly LinqToDbConnectionProvider _dbProvider;
    
    // Service injection
    private readonly PokemonService _pokemonService;
}
```

### Authentication Schemes

- **"ApiKey"**: Header-based API key authentication
- **"Jwt"**: JWT Bearer token for user sessions
- **Authorization Policies**: AdminPolicy, BotOwnerPolicy support role-based access

## Development Practices

### Error Handling

- Comprehensive try-catch in `CommandHandler` and `EventHandler`
- Structured logging via Serilog with context preservation
- Discord API error handling with retry logic

### Performance Optimization

- **Redis Caching**: Guild settings cached with 1-hour TTL
- **Database Connection Management**: LinqToDB with CloseAfterUse pattern
- **Sharded Architecture**: Multiple bot shards for scale
- **Command Queuing**: Prevents race conditions in high-traffic scenarios

### Testing and Development

- **Debug Mode**: Commands registered to DebugGuildId only
- **Production Mode**: Global command registration
- **Swagger UI**: Available at `/swagger` in development
- **Logging**: Console and file-based structured logging

## Important Configuration Notes

- **Gateway Intents**: Uses `GatewayIntents.All` - requires Discord application permissions
- **Sharding**: Automatically handles multiple shards based on guild count
- **Connection Strings**: All database connections configured via config.json
- **JWT Configuration**: Requires secure JWT secret for API authentication
- **CORS**: Configured for localhost development servers (ports 3000, 5173)

## Common Development Tasks

1. **Adding New Commands**: Create in appropriate `/Modules/` directory, inherit from `EeveeCoreSlashModuleBase`
2. **Database Changes**: Add migration SQL files to `/Database/Migrations/`
3. **API Endpoints**: Create controllers in `/Controllers/` with appropriate authorization
4. **Service Integration**: Register services with DI container, follow existing patterns
5. **Guild Settings**: Use `GuildSettingsService` with Redis caching
6. **Static Data**: Add to MongoDB via `IMongoService` for reference data

The codebase emphasizes modularity, performance, and maintainability with a clear separation between Discord bot functionality and web API services.