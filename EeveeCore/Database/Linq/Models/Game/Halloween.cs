using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Game;

/// <summary>
///     Represents a user's progress and resources in the Halloween seasonal event.
///     This class tracks event-specific currencies and participation.
/// </summary>
[Table("halloween")]
public class Halloween
{
    /// <summary>
    ///     Gets or sets the Discord user ID associated with this Halloween event record.
    ///     This serves as the primary key for the record.
    /// </summary>
    [PrimaryKey]
    [Column("u_id")]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the amount of candy collected by the user during the Halloween event.
    ///     Candy is likely a primary event currency used for rewards.
    /// </summary>
    [Column("candy")]
    [NotNull]
    public int Candy { get; set; }

    /// <summary>
    ///     Gets or sets the number of bones collected by the user during the Halloween event.
    ///     Bones may be a secondary event currency or collectible.
    /// </summary>
    [Column("bone")]
    [NotNull]
    public int Bone { get; set; }

    /// <summary>
    ///     Gets or sets the number of pumpkins collected by the user during the Halloween event.
    ///     Pumpkins may be a secondary event currency or collectible.
    /// </summary>
    [Column("pumpkin")]
    [NotNull]
    public int Pumpkin { get; set; }

    /// <summary>
    ///     Gets or sets the number of raffle tickets the user has for the Halloween event.
    /// </summary>
    [Column("raffle")]
    [NotNull]
    public int Raffle { get; set; }

    /// <summary>
    ///     Gets or sets the username of the participant, for easier reference in event rankings.
    /// </summary>
    [Column("username")]
    public string? Username { get; set; }
}