namespace EeveeCore.Modules.Duels.Impl.DuelPokemon;

public partial class DuelPokemon
{
    /// <summary>
    ///     Transforms this poke into otherpoke.
    ///     Copies all stats, moves, ability, and type from the target Pokemon.
    ///     Used by moves like Transform and abilities like Imposter.
    /// </summary>
    /// <param name="otherpoke">The Pokemon to transform into.</param>
    public void Transform(DuelPokemon otherpoke)
    {
        ChoiceMove = null;
        _name = otherpoke._name;
        if (_nickname != "None")
            Name = $"{_nickname} ({_name.Replace('-', ' ')})";
        else
            Name = _name.Replace("-", " ");
        Attack = otherpoke.Attack;
        Defense = otherpoke.Defense;
        SpAtk = otherpoke.SpAtk;
        SpDef = otherpoke.SpDef;
        Speed = otherpoke.Speed;
        HpIV = otherpoke.HpIV;
        AtkIV = otherpoke.AtkIV;
        DefIV = otherpoke.DefIV;
        SpAtkIV = otherpoke.SpAtkIV;
        SpDefIV = otherpoke.SpDefIV;
        SpeedIV = otherpoke.SpeedIV;
        HpEV = otherpoke.HpEV;
        AtkEV = otherpoke.AtkEV;
        DefEV = otherpoke.DefEV;
        SpAtkEV = otherpoke.SpAtkEV;
        SpDefEV = otherpoke.SpDefEV;
        SpeedEV = otherpoke.SpeedEV;
        Moves = otherpoke.Moves.Select(move => move.Copy()).ToList();
        foreach (var m in Moves) m.PP = 5;
        AbilityId = otherpoke.AbilityId;
        TypeIds = new List<ElementType>(otherpoke.TypeIds);
        AttackStage = otherpoke.AttackStage;
        DefenseStage = otherpoke.DefenseStage;
        SpAtkStage = otherpoke.SpAtkStage;
        SpDefStage = otherpoke.SpDefStage;
        SpeedStage = otherpoke.SpeedStage;
        AccuracyStage = otherpoke.AccuracyStage;
        EvasionStage = otherpoke.EvasionStage;
    }

    /// <summary>
    ///     Changes this poke's form to the provided form.
    ///     This changes its name and base stats, and may affect moves, abilities, and items.
    ///     Returns True if the poke successfully reformed.
    /// </summary>
    /// <param name="form">The form name to change to.</param>
    /// <returns>True if the form change was successful, false if the form doesn't exist.</returns>
    public bool Form(string? form)
    {
        if (!BaseStats.ContainsKey(form)) return false;
        _name = form;
        if (_nickname != "None")
            DisplayName = $"{_nickname} ({_name.Replace('-', ' ')})";
        else
            DisplayName = _name.Replace("-", " ");
        Attack = BaseStats[_name][1];
        Defense = BaseStats[_name][2];
        SpAtk = BaseStats[_name][3];
        SpDef = BaseStats[_name][4];
        Speed = BaseStats[_name][5];
        AttackSplit = null;
        SpAtkSplit = null;
        DefenseSplit = null;
        SpDefSplit = null;
        Autotomize = 0;
        return true;
    }
}