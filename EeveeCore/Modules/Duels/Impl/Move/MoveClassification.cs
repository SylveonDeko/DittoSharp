namespace EeveeCore.Modules.Duels.Impl.Move;

public partial class Move
{
    /// <summary>
    ///     Whether or not this move is sound based.
    /// </summary>
    public bool IsSoundBased()
    {
        return new[]
        {
            45, 46, 47, 48, 103, 173, 195, 215, 253, 304, 319, 320, 336, 405, 448, 496, 497, 547,
            555, 568, 574, 575, 586, 590, 664, 691, 728, 744, 753, 826, 871, 1005, 1006
        }.Contains(Id);
    }

    /// <summary>
    ///     Whether or not this move is a punching move.
    /// </summary>
    public bool IsPunching()
    {
        return new[]
        {
            4, 5, 7, 8, 9, 146, 183, 223, 264, 309, 325, 327, 359, 409, 418, 612, 665, 721, 729,
            764, 765, 834, 857, 889
        }.Contains(Id);
    }

    /// <summary>
    ///     Whether or not this move is a biting move.
    /// </summary>
    public bool IsBiting()
    {
        return new[] { 44, 158, 242, 305, 422, 423, 424, 706, 733, 742 }.Contains(Id);
    }

    /// <summary>
    ///     Whether or not this move is a ball or bomb move.
    /// </summary>
    public bool IsBallOrBomb()
    {
        return new[]
        {
            121, 140, 188, 190, 192, 247, 296, 301, 311, 331, 350, 360, 396, 402, 411, 412, 426,
            439, 443, 486, 491, 545, 676, 690, 748, 1017
        }.Contains(Id);
    }

    /// <summary>
    ///     Whether or not this move is an aura or pulse move.
    /// </summary>
    public bool IsAuraOrPulse()
    {
        return new[] { 352, 396, 399, 406, 505, 618, 805 }.Contains(Id);
    }

    /// <summary>
    ///     Whether or not this move is a powder or spore move.
    /// </summary>
    public bool IsPowderOrSpore()
    {
        return new[] { 77, 78, 79, 147, 178, 476, 600, 737 }.Contains(Id);
    }

    /// <summary>
    ///     Whether or not this move is a dance move.
    /// </summary>
    public bool IsDance()
    {
        return new[] { 14, 80, 297, 298, 349, 461, 483, 552, 686, 744, 846, 872 }.Contains(Id);
    }

    /// <summary>
    ///     Whether or not this move is a slicing move.
    /// </summary>
    public bool IsSlicing()
    {
        return new[]
        {
            15, 75, 163, 210, 314, 332, 348, 400, 403, 404, 427, 440, 533, 534, 669, 749, 830, 845,
            860, 869, 891, 895, 1013, 1014
        }.Contains(Id);
    }

    /// <summary>
    ///     Whether or not this move is a wind move.
    /// </summary>
    public bool IsWind()
    {
        return new[] { 16, 18, 59, 196, 201, 239, 257, 314, 366, 542, 572, 584, 829, 842, 844, 849 }.Contains(Id);
    }

    /// <summary>
    ///     Whether or not this move can be reflected by magic coat and magic bounce.
    /// </summary>
    public bool IsAffectedByMagicCoat()
    {
        return new[]
        {
            18, 28, 39, 43, 45, 46, 47, 48, 50, 73, 77, 78, 79, 81, 86, 92, 95, 103, 108, 109, 134,
            137, 139, 142, 147, 148, 169, 178, 180, 184, 186, 191, 193, 204, 207, 212, 213, 227, 230,
            259, 260, 261, 269, 281, 297, 313, 316, 319, 320, 321, 335, 357, 373, 377, 380, 388, 390,
            432, 445, 446, 464, 477, 487, 493, 494, 505, 564, 567, 568, 571, 575, 576, 589, 590, 598,
            599, 600, 608, 666, 668, 671, 672, 685, 715, 736, 737, 810
        }.Contains(Id);
    }

    /// <summary>
    ///     Whether or not this move cannot be selected during heal block.
    /// </summary>
    public bool IsAffectedByHealBlock()
    {
        return new[]
        {
            71, 72, 105, 135, 138, 141, 156, 202, 208, 234, 235, 236, 256, 273, 303, 355, 361, 409,
            456, 461, 505, 532, 570, 577, 613, 659, 666, 668, 685
        }.Contains(Id);
    }

    /// <summary>
    ///     Whether or not this move is able to bypass a substitute.
    /// </summary>
    public bool IsAffectedBySubstitute()
    {
        return !new[]
        {
            18, 45, 46, 47, 48, 50, 102, 103, 114, 166, 173, 174, 176, 180, 193, 195, 213, 215, 227,
            244, 253, 259, 269, 270, 272, 285, 286, 304, 312, 316, 319, 320, 357, 367, 382, 384, 385,
            391, 405, 448, 495, 496, 497, 513, 516, 547, 555, 568, 574, 575, 586, 587, 589, 590, 593,
            597, 600, 602, 607, 621, 664, 674, 683, 689, 691, 712, 728, 753, 826, 871, 1005, 1006
        }.Contains(Id);
    }

    /// <summary>
    ///     Whether or not this move targets the opponent.
    /// </summary>
    public bool TargetsOpponent()
    {
        // Moves which don't follow normal targeting protocols, ignore them unless they are damaging.
        if (Target == MoveTarget.SPECIFIC_MOVE && DamageClass == DamageClass.STATUS) return false;
        // Moves which do not target the opponent Pokemon.
        return !new[]
        {
            MoveTarget.SELECTED_POKEMON_ME_FIRST,
            MoveTarget.ALLY,
            MoveTarget.USERS_FIELD,
            MoveTarget.USER_OR_ALLY,
            MoveTarget.OPPONENTS_FIELD,
            MoveTarget.USER,
            MoveTarget.ENTIRE_FIELD,
            MoveTarget.USER_AND_ALLIES,
            MoveTarget.ALL_ALLIES
        }.Contains(Target);
    }

    /// <summary>
    ///     Whether or not this move targets multiple Pokemon.
    /// </summary>
    public bool TargetsMultiple()
    {
        return new[]
        {
            MoveTarget.ALL_OTHER_POKEMON,
            MoveTarget.ALL_OPPONENTS,
            MoveTarget.USER_AND_ALLIES,
            MoveTarget.ALL_POKEMON,
            MoveTarget.ALL_ALLIES
        }.Contains(Target);
    }

    /// <summary>
    ///     Whether or not this move makes contact.
    /// </summary>
    public bool MakesContact(DuelPokemon.DuelPokemon attacker)
    {
        var makesContact = new[]
        {
            1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 15, 17, 19, 20, 21, 22, 23, 24, 25, 26, 27, 29,
            30, 31, 32, 33, 34, 35, 36, 37, 38, 44, 64, 65, 66, 67, 68, 69, 70, 80, 91, 98, 99,
            117, 122, 127, 128, 130, 132, 136, 141, 146, 152, 154, 158, 162, 163, 165, 167, 168,
            172, 175, 179, 183, 185, 200, 205, 206, 209, 210, 211, 216, 218, 223, 224, 228, 229,
            231, 232, 233, 238, 242, 245, 249, 252, 263, 264, 265, 276, 279, 280, 282, 283, 291,
            292, 299, 301, 302, 305, 306, 309, 310, 325, 327, 332, 337, 340, 342, 343, 344, 348,
            358, 359, 360, 365, 369, 370, 371, 372, 376, 378, 386, 387, 389, 394, 395, 398, 400,
            401, 404, 407, 409, 413, 416, 418, 419, 421, 422, 423, 424, 425, 428, 431, 438, 440,
            442, 447, 450, 452, 453, 457, 458, 462, 467, 480, 484, 488, 490, 492, 498, 507, 509,
            512, 514, 525, 528, 529, 530, 531, 532, 533, 534, 535, 537, 541, 543, 544, 550, 557,
            560, 565, 566, 577, 583, 609, 610, 611, 612, 620, 658, 660, 663, 665, 667, 669, 675,
            677, 679, 680, 681, 684, 688, 692, 693, 696, 699, 701, 706, 707, 709, 710, 712, 713,
            716, 718, 721, 724, 729, 730, 733, 741, 742, 745, 747, 749, 750, 752, 756, 760, 764,
            765, 766, 779, 799, 803, 806, 812, 813, 821, 830, 832, 834, 840, 845, 848, 853, 857,
            859, 860, 861, 862, 866, 869, 872, 873, 878, 879, 884, 885, 887, 889, 891, 892, 894,
            1003, 1010, 1012, 1013
        }.Contains(Id);

        return makesContact && attacker.Ability() != Ability.LONG_REACH;
    }
}