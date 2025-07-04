using EeveeCore.Common.Enums;

namespace EeveeCore.Common.Attributes.Interactions;

/// <summary>
///     Attribute to check if user has Helper rank or higher before executing a command or method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequireHelperAttribute : RequireStaffAttribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RequireHelperAttribute" /> class.
    /// </summary>
    public RequireHelperAttribute() : base(StaffRank.Helper)
    {
    }
}