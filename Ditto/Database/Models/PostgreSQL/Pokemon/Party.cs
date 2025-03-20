using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Pokemon;

[Table("partys")]
public class Party
{
    [Key] [Column("p_id")] public int PartyId { get; set; }

    [Column("u_id")] [Required] public ulong UserId { get; set; }

    [Column("slot1")] public ulong? Slot1 { get; set; }

    [Column("slot2")] public ulong? Slot2 { get; set; }

    [Column("slot3")] public ulong? Slot3 { get; set; }

    [Column("slot4")] public ulong? Slot4 { get; set; }

    [Column("slot5")] public ulong? Slot5 { get; set; }

    [Column("slot6")] public ulong? Slot6 { get; set; }

    [Column("name")] public string? Name { get; set; }

    [Column("quick")] [Required] public bool Quick { get; set; }
}