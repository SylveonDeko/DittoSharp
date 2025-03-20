// File: MoveSelectionUtility.cs

namespace Ditto.Modules.Duels.Impl.Move;

public partial class Move
{
    /// <summary>
    ///     Whether or not this move can be selected by mirror move.
    /// </summary>
    public bool SelectableByMirrorMove()
    {
        return TargetsOpponent();
    }

    /// <summary>
    ///     Whether or not this move can be selected by sleep talk.
    /// </summary>
    public bool SelectableBySleepTalk()
    {
        return !new[]
        {
            13, 19, 76, 91, 102, 117, 118, 119, 130, 143, 166, 253, 264, 274, 291, 340, 382, 383,
            467, 507, 553, 554, 562, 566, 601, 669, 690, 704, 731
        }.Contains(Id);
    }

    /// <summary>
    ///     Whether or not this move can be selected by assist.
    /// </summary>
    public bool SelectableByAssist()
    {
        return !new[]
        {
            18, 19, 46, 68, 91, 102, 118, 119, 144, 165, 166, 168, 182, 194, 197, 203, 214, 243,
            264, 266, 267, 270, 271, 289, 291, 340, 343, 364, 382, 383, 415, 448, 467, 476, 507,
            509, 516, 525, 561, 562, 566, 588, 596, 606, 607, 661, 671, 690, 704
        }.Contains(Id);
    }

    /// <summary>
    ///     Whether or not this move can be selected by mimic.
    /// </summary>
    public bool SelectableByMimic()
    {
        return !new[] { 102, 118, 165, 166, 448, 896 }.Contains(Id);
    }

    /// <summary>
    ///     Whether or not this move can be selected by instruct.
    /// </summary>
    public bool SelectableByInstruct()
    {
        return !new[]
        {
            13, 19, 63, 76, 91, 102, 117, 118, 119, 130, 143, 144, 165, 166, 214, 264, 267, 274,
            289, 291, 307, 308, 338, 340, 382, 383, 408, 416, 439, 459, 467, 507, 553, 554, 566,
            588, 601, 669, 689, 690, 704, 711, 761, 762, 896
        }.Contains(Id);
    }

    /// <summary>
    ///     Whether or not this move can be selected by snatch.
    /// </summary>
    public bool SelectableBySnatch()
    {
        return new[]
        {
            14, 54, 74, 96, 97, 104, 105, 106, 107, 110, 111, 112, 113, 115, 116, 133, 135, 151,
            156, 159, 160, 164, 187, 208, 215, 219, 234, 235, 236, 254, 256, 268, 273, 275, 278,
            286, 287, 293, 294, 303, 312, 322, 334, 336, 339, 347, 349, 355, 361, 366, 379, 381,
            392, 393, 397, 417, 455, 456, 461, 468, 469, 475, 483, 489, 501, 504, 508, 526, 538,
            561, 602, 659, 673, 674, 694, 0xCFCF
        }.Contains(Id);
    }
}