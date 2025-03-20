using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Game;

[Table("market")]
public class Market
{
    [Key] [Column("id")] public ulong Id { get; set; }

    [Column("poke")] [Required] public int PokemonId { get; set; }

    [Column("owner")] [Required] public ulong OwnerId { get; set; }

    [Column("price")] [Required] public int Price { get; set; }

    [Column("buyer")] public ulong? BuyerId { get; set; }
}