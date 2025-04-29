using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Game;

/// <summary>
///     Represents a user's redeem token purchase information in the EeveeCore Pokémon bot system.
///     This class tracks redeem token purchases and store restock timing.
/// </summary>
[Table("redeemstore")]
public class RedeemStore
{
    /// <summary>
    ///     Gets or sets the Discord user ID associated with this redeem store record.
    ///     This serves as the primary key for the record.
    /// </summary>
    [Key]
    [Column("u_id")]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the number of redeem tokens the user has purchased in the current cycle.
    /// </summary>
    [Column("bought")]
    [Required]
    public int Bought { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the redeem store will restock.
    ///     The store likely has a purchase limit that resets periodically.
    /// </summary>
    [Column("restock")]
    [Required]
    public string Restock { get; set; } = "0";
}