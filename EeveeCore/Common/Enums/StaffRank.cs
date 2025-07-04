namespace EeveeCore.Common.Enums;

/// <summary>
///     Represents the staff rank hierarchy in the EeveeCore system.
///     Higher values indicate more access and permissions.
/// </summary>
public enum StaffRank
{
    /// <summary>
    ///     Regular user with no staff permissions.
    /// </summary>
    User = 0,

    /// <summary>
    ///     AI staff member with basic permissions.
    /// </summary>
    AI = 1,

    /// <summary>
    ///     Support staff member.
    /// </summary>
    Support = 2,

    /// <summary>
    ///     Gym staff member.
    /// </summary>
    Gym = 3,

    /// <summary>
    ///     Helper staff member.
    /// </summary>
    Helper = 4,

    /// <summary>
    ///     Moderator staff member.
    /// </summary>
    Mod = 5,

    /// <summary>
    ///     Gym authority staff member.
    /// </summary>
    GymAuth = 6,

    /// <summary>
    ///     Investigator staff member.
    /// </summary>
    Investigator = 7,

    /// <summary>
    ///     Administrator staff member.
    /// </summary>
    Admin = 8,

    /// <summary>
    ///     Developer with full system access.
    /// </summary>
    Developer = 9
}