using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Pokemon;

[Table("mothers")]
public class Mother
{
    [Key] [Column("pokemon_id")] public ulong PokemonId { get; set; }

    [Column("owner")] public ulong? OwnerId { get; set; }

    [Column("entry_time")] public DateTime? EntryTime { get; set; }
}