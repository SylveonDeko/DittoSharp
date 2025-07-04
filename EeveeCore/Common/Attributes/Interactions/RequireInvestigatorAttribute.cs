using EeveeCore.Common.Enums;

namespace EeveeCore.Common.Attributes.Interactions;

/// <summary>
///     Attribute to check if user has Investigator rank or higher before executing a command or method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequireInvestigatorAttribute : RequireStaffAttribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RequireInvestigatorAttribute" /> class.
    /// </summary>
    public RequireInvestigatorAttribute() : base(StaffRank.Investigator)
    {
    }
}