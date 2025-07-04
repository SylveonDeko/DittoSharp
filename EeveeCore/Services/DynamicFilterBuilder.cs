using System.Linq.Expressions;
using EeveeCore.Database.Linq.Models.Bot;
using Serilog;

namespace EeveeCore.Services;

/// <summary>
///     Service for building dynamic LINQ queries from user-defined filter criteria.
///     Converts UserFilterCriteria into executable database queries.
/// </summary>
public class DynamicFilterBuilder
{
    /// <summary>
    ///     Applies filter criteria to a Pokemon query.
    /// </summary>
    /// <param name="query">The base Pokemon query.</param>
    /// <param name="criteria">List of filter criteria to apply.</param>
    /// <returns>The filtered query.</returns>
    public static IQueryable<T> ApplyFilterCriteria<T>(IQueryable<T> query, List<UserFilterCriteria> criteria)
        where T : class
    {
        if (criteria == null || !criteria.Any())
            return query;

        try
        {
            // Group criteria by logical operators (AND/OR)
            var criteriaGroups = GroupCriteriaByLogic(criteria);
            
            Expression<Func<T, bool>>? combinedPredicate = null;

            foreach (var group in criteriaGroups)
            {
                var groupPredicate = BuildGroupPredicate<T>(group.Criteria);
                
                if (groupPredicate == null)
                    continue;

                if (combinedPredicate == null)
                {
                    combinedPredicate = groupPredicate;
                }
                else
                {
                    // Combine with the previous predicate using the logical operator
                    if (group.LogicalOperator == "OR")
                        combinedPredicate = CombinePredicates(combinedPredicate, groupPredicate, Expression.OrElse);
                    else
                        combinedPredicate = CombinePredicates(combinedPredicate, groupPredicate, Expression.AndAlso);
                }
            }

            return combinedPredicate != null ? query.Where(combinedPredicate) : query;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error applying filter criteria to query");
            return query; // Return unfiltered query on error
        }
    }

    /// <summary>
    ///     Groups criteria by their logical operators for proper AND/OR handling.
    /// </summary>
    private static List<CriteriaGroup> GroupCriteriaByLogic(List<UserFilterCriteria> criteria)
    {
        var groups = new List<CriteriaGroup>();
        var currentGroup = new List<UserFilterCriteria>();
        var currentOperator = "AND";

        foreach (var criterion in criteria.OrderBy(c => c.CriterionOrder))
        {
            currentGroup.Add(criterion);

            // If this criterion has a logical connector, it determines how to combine with the next group
            if (criterion.LogicalConnector != null)
            {
                groups.Add(new CriteriaGroup { Criteria = new List<UserFilterCriteria>(currentGroup), LogicalOperator = currentOperator });
                currentGroup.Clear();
                currentOperator = criterion.LogicalConnector.ToUpper();
            }
        }

        // Add the final group
        if (currentGroup.Any())
        {
            groups.Add(new CriteriaGroup { Criteria = currentGroup, LogicalOperator = currentOperator });
        }

        return groups;
    }

    /// <summary>
    ///     Builds a predicate for a group of criteria (all AND-ed together).
    /// </summary>
    private static Expression<Func<T, bool>>? BuildGroupPredicate<T>(List<UserFilterCriteria> criteria)
        where T : class
    {
        if (!criteria.Any())
            return null;

        Expression<Func<T, bool>>? groupPredicate = null;

        foreach (var criterion in criteria)
        {
            var predicate = BuildSinglePredicate<T>(criterion);
            if (predicate == null)
                continue;

            if (groupPredicate == null)
                groupPredicate = predicate;
            else
                groupPredicate = CombinePredicates(groupPredicate, predicate, Expression.AndAlso);
        }

        return groupPredicate;
    }

    /// <summary>
    ///     Builds a predicate for a single filter criterion.
    /// </summary>
    private static Expression<Func<T, bool>>? BuildSinglePredicate<T>(UserFilterCriteria criterion)
        where T : class
    {
        try
        {
            var parameter = Expression.Parameter(typeof(T), "p");
            var property = GetPropertyExpression(parameter, criterion.FieldName);
            
            if (property == null)
                return null;

            var comparison = BuildComparisonExpression(property, criterion);
            if (comparison == null)
                return null;

            return Expression.Lambda<Func<T, bool>>(comparison, parameter);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to build predicate for criterion {FieldName} {Operator}", 
                criterion.FieldName, criterion.Operator);
            return null;
        }
    }

    /// <summary>
    ///     Gets the property expression for a field name, handling nested properties.
    /// </summary>
    private static Expression? GetPropertyExpression(ParameterExpression parameter, string fieldName)
    {
        try
        {
            var propertyPath = GetPropertyPath(fieldName);
            if (propertyPath == null)
                return null;

            Expression current = parameter;
            
            // Navigate through the property path (e.g., "Pokemon.Level" for joined queries)
            foreach (var propertyName in propertyPath.Split('.'))
            {
                var property = current.Type.GetProperty(propertyName, 
                    System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (property == null)
                    return null;

                current = Expression.Property(current, property);
            }

            return current;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Maps filter field names to actual property paths.
    /// </summary>
    private static string? GetPropertyPath(string fieldName)
    {
        return fieldName.ToLower() switch
        {
            "level" => "Pokemon.Level",
            "hp_iv" => "Pokemon.HpIv",
            "attack_iv" => "Pokemon.AttackIv", 
            "defense_iv" => "Pokemon.DefenseIv",
            "special_attack_iv" => "Pokemon.SpecialAttackIv",
            "special_defense_iv" => "Pokemon.SpecialDefenseIv",
            "speed_iv" => "Pokemon.SpeedIv",
            "shiny" => "Pokemon.Shiny",
            "radiant" => "Pokemon.Radiant",
            "nature" => "Pokemon.Nature",
            "gender" => "Pokemon.Gender",
            "caught_at" => "Pokemon.Timestamp",
            "favorite" => "Pokemon.Favorite",
            "champion" => "Pokemon.Champion",
            "tradable" => "Pokemon.Tradable",
            "breedable" => "Pokemon.Breedable",
            "pokemon_name" => "Pokemon.PokemonName",
            "nickname" => "Pokemon.Nickname",
            "held_item" => "Pokemon.HeldItem",
            "happiness" => "Pokemon.Happiness",
            "experience" => "Pokemon.Experience",
            "tags" => "Pokemon.Tags",
            "moves" => "Pokemon.Moves",
            "skin" => "Pokemon.Skin",
            "market_enlist" => "Pokemon.MarketEnlist",
            // Special calculated fields
            "iv_total" => null, // Handled specially
            "iv_percentage" => null, // Handled specially
            "type" => null, // Handled specially (requires MongoDB lookup)
            _ => null
        };
    }

    /// <summary>
    ///     Builds a comparison expression based on the criterion operator.
    /// </summary>
    private static Expression? BuildComparisonExpression(Expression property, UserFilterCriteria criterion)
    {
        try
        {
            // Handle special cases first
            if (criterion.FieldName.ToLower() == "iv_total")
                return BuildIvTotalComparison(property, criterion);
            
            if (criterion.FieldName.ToLower() == "iv_percentage")
                return BuildIvPercentageComparison(property, criterion);

            var value = GetComparisonValue(property.Type, criterion);
            if (value == null && !IsNullCheckOperator(criterion.Operator))
                return null;

            return criterion.Operator.ToLower() switch
            {
                "equals" => Expression.Equal(property, Expression.Constant(value, property.Type)),
                "not_equals" => Expression.NotEqual(property, Expression.Constant(value, property.Type)),
                "greater_than" => Expression.GreaterThan(property, Expression.Constant(value, property.Type)),
                "less_than" => Expression.LessThan(property, Expression.Constant(value, property.Type)),
                "greater_equal" => Expression.GreaterThanOrEqual(property, Expression.Constant(value, property.Type)),
                "less_equal" => Expression.LessThanOrEqual(property, Expression.Constant(value, property.Type)),
                "between" => BuildBetweenExpression(property, criterion),
                "contains" => BuildContainsExpression(property, criterion),
                "not_contains" => Expression.Not(BuildContainsExpression(property, criterion) ?? Expression.Constant(false)),
                "in_list" => BuildInListExpression(property, criterion),
                "not_in_list" => Expression.Not(BuildInListExpression(property, criterion) ?? Expression.Constant(false)),
                "is_null" => Expression.Equal(property, Expression.Constant(null, property.Type)),
                "is_not_null" => Expression.NotEqual(property, Expression.Constant(null, property.Type)),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Builds IV total comparison (sum of all IV values).
    /// </summary>
    private static Expression? BuildIvTotalComparison(Expression parameter, UserFilterCriteria criterion)
    {
        try
        {
            // Build expression: HpIv + AttackIv + DefenseIv + SpecialAttackIv + SpecialDefenseIv + SpeedIv
            var pokemonParam = parameter.Type.Name == "Pokemon" ? parameter : 
                Expression.Property(parameter, "Pokemon");

            var hpIv = Expression.Property(pokemonParam, "HpIv");
            var attackIv = Expression.Property(pokemonParam, "AttackIv");
            var defenseIv = Expression.Property(pokemonParam, "DefenseIv");
            var spAttackIv = Expression.Property(pokemonParam, "SpecialAttackIv");
            var spDefenseIv = Expression.Property(pokemonParam, "SpecialDefenseIv");
            var speedIv = Expression.Property(pokemonParam, "SpeedIv");

            var totalIv = Expression.Add(
                Expression.Add(
                    Expression.Add(hpIv, attackIv),
                    Expression.Add(defenseIv, spAttackIv)
                ),
                Expression.Add(spDefenseIv, speedIv)
            );

            var value = Expression.Constant(criterion.ValueNumeric ?? 0);

            return criterion.Operator.ToLower() switch
            {
                "equals" => Expression.Equal(totalIv, value),
                "not_equals" => Expression.NotEqual(totalIv, value),
                "greater_than" => Expression.GreaterThan(totalIv, value),
                "less_than" => Expression.LessThan(totalIv, value),
                "greater_equal" => Expression.GreaterThanOrEqual(totalIv, value),
                "less_equal" => Expression.LessThanOrEqual(totalIv, value),
                "between" when criterion.ValueNumericMax.HasValue => 
                    Expression.AndAlso(
                        Expression.GreaterThanOrEqual(totalIv, value),
                        Expression.LessThanOrEqual(totalIv, Expression.Constant(criterion.ValueNumericMax.Value))
                    ),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Builds IV percentage comparison (IV total / 186 * 100).
    /// </summary>
    private static Expression? BuildIvPercentageComparison(Expression parameter, UserFilterCriteria criterion)
    {
        try
        {
            var ivTotalExpr = BuildIvTotalComparison(parameter, new UserFilterCriteria 
            { 
                FieldName = "iv_total", 
                Operator = "equals", 
                ValueNumeric = 0 
            });
            
            if (ivTotalExpr == null)
                return null;

            // Convert to percentage: (ivTotal / 186.0) * 100
            var totalIv = ((BinaryExpression)ivTotalExpr).Left; // Extract the IV total expression
            var percentage = Expression.Multiply(
                Expression.Divide(
                    Expression.Convert(totalIv, typeof(double)),
                    Expression.Constant(186.0)
                ),
                Expression.Constant(100.0)
            );

            var value = Expression.Constant((double)(criterion.ValueNumeric ?? 0));

            return criterion.Operator.ToLower() switch
            {
                "equals" => Expression.Equal(percentage, value),
                "greater_than" => Expression.GreaterThan(percentage, value),
                "less_than" => Expression.LessThan(percentage, value),
                "greater_equal" => Expression.GreaterThanOrEqual(percentage, value),
                "less_equal" => Expression.LessThanOrEqual(percentage, value),
                "between" when criterion.ValueNumericMax.HasValue => 
                    Expression.AndAlso(
                        Expression.GreaterThanOrEqual(percentage, value),
                        Expression.LessThanOrEqual(percentage, Expression.Constant((double)criterion.ValueNumericMax.Value))
                    ),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Builds a between expression for range comparisons.
    /// </summary>
    private static Expression? BuildBetweenExpression(Expression property, UserFilterCriteria criterion)
    {
        if (!criterion.ValueNumeric.HasValue || !criterion.ValueNumericMax.HasValue)
            return null;

        var minValue = Expression.Constant(criterion.ValueNumeric.Value, property.Type);
        var maxValue = Expression.Constant(criterion.ValueNumericMax.Value, property.Type);

        return Expression.AndAlso(
            Expression.GreaterThanOrEqual(property, minValue),
            Expression.LessThanOrEqual(property, maxValue)
        );
    }

    /// <summary>
    ///     Builds a contains expression for string properties.
    /// </summary>
    private static Expression? BuildContainsExpression(Expression property, UserFilterCriteria criterion)
    {
        if (string.IsNullOrEmpty(criterion.ValueText))
            return null;

        if (property.Type == typeof(string) || Nullable.GetUnderlyingType(property.Type) == typeof(string))
        {
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            if (containsMethod == null)
                return null;

            return Expression.Call(property, containsMethod, Expression.Constant(criterion.ValueText));
        }

        // Handle string arrays (tags, moves)
        if (property.Type == typeof(string[]) || Nullable.GetUnderlyingType(property.Type) == typeof(string[]))
        {
            var anyMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(string));

            var itemParam = Expression.Parameter(typeof(string), "item");
            var containsExpr = Expression.Call(itemParam, "Contains", null, Expression.Constant(criterion.ValueText));
            var lambda = Expression.Lambda<Func<string, bool>>(containsExpr, itemParam);

            return Expression.Call(anyMethod, property, lambda);
        }

        return null;
    }

    /// <summary>
    ///     Builds an "in list" expression.
    /// </summary>
    private static Expression? BuildInListExpression(Expression property, UserFilterCriteria criterion)
    {
        if (string.IsNullOrEmpty(criterion.ValueText))
            return null;

        var values = criterion.ValueText.Split(',').Select(v => v.Trim()).ToArray();
        if (!values.Any())
            return null;

        // Convert values to the appropriate type
        var convertedValues = Array.CreateInstance(property.Type, values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            try
            {
                convertedValues.SetValue(Convert.ChangeType(values[i], property.Type), i);
            }
            catch
            {
                return null; // Invalid conversion
            }
        }

        var containsMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(property.Type);

        var valueArray = Expression.Constant(convertedValues);
        return Expression.Call(containsMethod, valueArray, property);
    }

    /// <summary>
    ///     Gets the comparison value for a criterion based on the property type.
    /// </summary>
    private static object? GetComparisonValue(Type propertyType, UserFilterCriteria criterion)
    {
        try
        {
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (underlyingType == typeof(string))
                return criterion.ValueText;

            if (underlyingType == typeof(bool))
                return criterion.ValueBoolean;

            if (underlyingType == typeof(int))
                return criterion.ValueNumeric;

            if (underlyingType == typeof(double) || underlyingType == typeof(float))
                return criterion.ValueNumeric?.ToString();

            if (underlyingType == typeof(DateTime))
            {
                if (DateTime.TryParse(criterion.ValueText, out var dateTime))
                    return dateTime;
            }

            if (underlyingType == typeof(ulong))
            {
                if (ulong.TryParse(criterion.ValueText, out var ulongValue))
                    return ulongValue;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Checks if an operator is a null check (doesn't require a value).
    /// </summary>
    private static bool IsNullCheckOperator(string operatorName)
    {
        return operatorName.ToLower() is "is_null" or "is_not_null";
    }

    /// <summary>
    ///     Combines two predicates with a specified logical operator.
    /// </summary>
    private static Expression<Func<T, bool>> CombinePredicates<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, BinaryExpression> combiner)
    {
        var parameter = Expression.Parameter(typeof(T), "p");
        var leftBody = ReplaceParameter(left.Body, left.Parameters[0], parameter);
        var rightBody = ReplaceParameter(right.Body, right.Parameters[0], parameter);
        var combined = combiner(leftBody, rightBody);
        
        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    /// <summary>
    ///     Replaces parameter references in an expression.
    /// </summary>
    private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParam, ParameterExpression newParam)
    {
        return new ParameterReplacer(oldParam, newParam).Visit(expression);
    }

    /// <summary>
    ///     Helper class for criteria grouping.
    /// </summary>
    private class CriteriaGroup
    {
        public List<UserFilterCriteria> Criteria { get; set; } = new();
        public string LogicalOperator { get; set; } = "AND";
    }

    /// <summary>
    ///     Expression visitor for replacing parameter references.
    /// </summary>
    private class ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == oldParam ? newParam : base.VisitParameter(node);
        }
    }
}