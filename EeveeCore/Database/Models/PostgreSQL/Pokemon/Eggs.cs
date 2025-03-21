using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Pokemon;

[Table("eggs")]
public class Eggs
{
    [Key] [Column("u_id")] public long UserId { get; set; }

    [Column("bidoof")] public int Bidoof { get; set; }

    [Column("caterpie")] public int Caterpie { get; set; }

    [Column("pidgey")] public int Pidgey { get; set; }

    [Column("magikarp")] public int Magikarp { get; set; }

    [Column("spinarak")] public int Spinarak { get; set; }

    [Column("tentacruel")] public int Tentacruel { get; set; }

    [Column("togepi")] public int Togepi { get; set; }

    [Column("bellsprout")] public int Bellsprout { get; set; }

    [Column("chansey")] public int Chansey { get; set; }

    [Column("omastar")] public int Omastar { get; set; }

    [Column("cubone")] public int Cubone { get; set; }

    [Column("farfetchd")] public int Farfetchd { get; set; }

    [Column("porygon")] public int Porygon { get; set; }

    [Column("ralts")] public int Ralts { get; set; }

    [Column("dratini")] public int Dratini { get; set; }

    [Column("larvitar")] public int Larvitar { get; set; }

    [Column("bagon")] public int Bagon { get; set; }

    [Column("gible")] public int Gible { get; set; }

    [Column("kyogre")] public int Kyogre { get; set; }

    [Column("dialga")] public int Dialga { get; set; }

    [Column("got_radiant")] public bool GotRadiant { get; set; }
}