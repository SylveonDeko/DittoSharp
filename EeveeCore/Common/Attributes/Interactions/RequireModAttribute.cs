using EeveeCore.Common.Enums;

namespace EeveeCore.Common.Attributes.Interactions;

/// <summary>
///     Attribute to check if user has Moderator rank or higher before executing a command or method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequireModAttribute : RequireStaffAttribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RequireModAttribute" /> class.
    /// </summary>
    public RequireModAttribute() : base(StaffRank.Mod)
    {
    }
}