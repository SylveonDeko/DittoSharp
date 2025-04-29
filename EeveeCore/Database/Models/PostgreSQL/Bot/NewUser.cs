using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

/// <summary>
///     Represents a new user in the EeveeCore Pokémon bot system and tracks their onboarding progress.
///     This class maintains the completion status of various tutorial tasks and guides new users through the system.
/// </summary>
[Table("new_users")]
public class NewUser
{
    /// <summary>
    ///     Gets or sets the Discord user ID of the new user.
    ///     This is the primary key for the table.
    /// </summary>
    [Key]
    [Column("u_id")]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has completed all onboarding tasks.
    /// </summary>
    [Column("completed")]
    public bool? Completed { get; set; }

    /// <summary>
    ///     Gets or sets the completion status of Task 1 tutorials as a JSON string.
    ///     Task 1 covers basic account features (bag, balance, hunt, region, trainer nickname).
    /// </summary>
    [Column("task1", TypeName = "jsonb")]
    public string? Task1 { get; set; } =
        "{\"bag\": false, \"bal\": false, \"hunt\": false, \"region\": false, \"trainernick\": false}";

    /// <summary>
    ///     Gets or sets the completion status of Task 2 tutorials as a JSON string.
    ///     Task 2 covers community and information features (info, vote, invite, updates, leaderboard).
    /// </summary>
    [Column("task2", TypeName = "jsonb")]
    public string? Task2 { get; set; } =
        "{\"info\": false, \"vote\": false, \"invite\": false, \"updates\": false, \"leaderboard\": false}";

    /// <summary>
    ///     Gets or sets the completion status of Task 3 tutorials as a JSON string.
    ///     Task 3 covers server and channel configurations (server, spawns, silence, redirect, botcommands).
    /// </summary>
    [Column("task3", TypeName = "jsonb")]
    public string? Task3 { get; set; } =
        "{\"server\": false, \"spawns\": false, \"silence\": false, \"redirect\": false, \"botcommands\": false}";

    /// <summary>
    ///     Gets or sets the completion status of Task 4 tutorials as a JSON string.
    ///     Task 4 covers Pokémon management commands (/f, /p, /fav, /tags, /order, /party, /select, /pokedex).
    /// </summary>
    [Column("task4", TypeName = "jsonb")]
    public string? Task4 { get; set; } =
        "{\"/f\": false, \"/p\": false, \"/fav\": {\"add\": false, \"list\": false, \"remove\": false}, \"/tags\": {\"add\": false, \"list\": false, \"remove\": false}, \"/order\": {\"evs\": false, \"ivs\": false, \"name\": false, \"level\": false, \"default\": false}, \"/party\": {\"add\": false, \"list\": false, \"load\": false, \"view\": false, \"setup\": false, \"remove\": false, \"register\": false, \"deregister\": false}, \"/select\": false, \"/pokedex\": false}";

    /// <summary>
    ///     Gets or sets the completion status of Task 5 tutorials as a JSON string.
    ///     Task 5 covers gameplay activities (/duel, /fish, /breed, /missions).
    /// </summary>
    [Column("task5", TypeName = "jsonb")]
    public string? Task5 { get; set; } =
        "{\"/duel\": {\"npc\": false, \"party\": false, \"single\": false, \"inverse\": false}, \"/fish\": false, \"/breed\": false, \"/missions\": false}";

    /// <summary>
    ///     Gets or sets the completion status of Task 6 tutorials as a JSON string.
    ///     Task 6 covers the economy and trading system (/m market commands, /buy, /gift, /sell, /shop, /trade).
    /// </summary>
    [Column("task6", TypeName = "jsonb")]
    public string? Task6 { get; set; } =
        "{\"/m\": {\"market_add\": false, \"market_buy\": false, \"market_info\": false, \"market_remove\": false}, \"/buy\": {\"item\": false, \"candy\": false, \"chest\": false, \"daycare\": false, \"redeems\": false, \"vitamins\": false}, \"/gift\": {\"credits\": false, \"pokemon\": false, \"redeems\": false}, \"/sell\": {\"egg\": false, \"item\": false}, \"/shop\": false, \"/trade\": false}";

    /// <summary>
    ///     Gets or sets a value indicating whether the user has completed all tutorial tasks.
    /// </summary>
    [Column("alltasks")]
    public bool? AllTasks { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has opted out of the tutorial system.
    /// </summary>
    [Column("opt_out")]
    public bool? OptOut { get; set; }
}