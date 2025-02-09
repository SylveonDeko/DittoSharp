using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Bot;

[Table("new_users")]
public class NewUser
{
    [Key]
    [Column("u_id")]
    public ulong UserId { get; set; }
    
    [Column("completed")]
    public bool? Completed { get; set; }
    
    [Column("task1", TypeName = "jsonb")]
    public string? Task1 { get; set; } = "{\"bag\": false, \"bal\": false, \"hunt\": false, \"region\": false, \"trainernick\": false}";
    
    [Column("task2", TypeName = "jsonb")]
    public string? Task2 { get; set; } = "{\"info\": false, \"vote\": false, \"invite\": false, \"updates\": false, \"leaderboard\": false}";
    
    [Column("task3", TypeName = "jsonb")]
    public string? Task3 { get; set; } = "{\"server\": false, \"spawns\": false, \"silence\": false, \"redirect\": false, \"botcommands\": false}";
    
    [Column("task4", TypeName = "jsonb")]
    public string? Task4 { get; set; } = "{\"/f\": false, \"/p\": false, \"/fav\": {\"add\": false, \"list\": false, \"remove\": false}, \"/tags\": {\"add\": false, \"list\": false, \"remove\": false}, \"/order\": {\"evs\": false, \"ivs\": false, \"name\": false, \"level\": false, \"default\": false}, \"/party\": {\"add\": false, \"list\": false, \"load\": false, \"view\": false, \"setup\": false, \"remove\": false, \"register\": false, \"deregister\": false}, \"/select\": false, \"/pokedex\": false}";
    
    [Column("task5", TypeName = "jsonb")]
    public string? Task5 { get; set; } = "{\"/duel\": {\"npc\": false, \"party\": false, \"single\": false, \"inverse\": false}, \"/fish\": false, \"/breed\": false, \"/missions\": false}";
    
    [Column("task6", TypeName = "jsonb")]
    public string? Task6 { get; set; } = "{\"/m\": {\"market_add\": false, \"market_buy\": false, \"market_info\": false, \"market_remove\": false}, \"/buy\": {\"item\": false, \"candy\": false, \"chest\": false, \"daycare\": false, \"redeems\": false, \"vitamins\": false}, \"/gift\": {\"credits\": false, \"pokemon\": false, \"redeems\": false}, \"/sell\": {\"egg\": false, \"item\": false}, \"/shop\": false, \"/trade\": false}";
    
    [Column("alltasks")]
    public bool? AllTasks { get; set; }
    
    [Column("opt_out")]
    public bool? OptOut { get; set; }
}