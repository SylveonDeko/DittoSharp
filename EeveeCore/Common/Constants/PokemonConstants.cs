namespace EeveeCore.Common.Constants;

public static class PokemonConstants
{
    // All Pokemon Lists combined
    public static readonly List<string> TotalList = PseudoAndStarters.PseudoList
        .Concat(PseudoAndStarters.StarterList)
        .Concat(LegendaryPokemon.LegendList)
        .Concat(LegendaryPokemon.UltraBeasts)
        .ToList();

    // Discord Emotes
    public static readonly string[] Emotes =
    [
        "<a:EeveeCoreslots:1217020409120292887>",
        "<a:loading:1130356999868129300><a:loading2:1130357001453588592>",
        "<a:2_LoadingRed:1101661849759526942>"
    ];

    public static bool IsFormed(string name)
    {
        var formSuffixes = new[]
        {
            "-bug", "-summer", "-marine", "-elegant", "-poison", "-average", "-altered",
            "-winter", "-trash", "-incarnate", "-baile", "-rainy", "-steel", "-star",
            "-ash", "-diamond", "-pop-star", "-fan", "-school", "-therian", "-pau",
            "-river", "-poke-ball", "-kabuki", "-electric", "-heat", "-unbound",
            "-chill", "-archipelago", "-zen", "-normal", "-mega-y", "-resolute",
            "-blade", "-speed", "-indigo", "-dusk", "-sky", "-west", "-sun", "-dandy",
            "-solo", "-high-plains", "-la-reine", "-50", "-unova-cap", "-burn",
            "-mega-x", "-monsoon", "-primal", "-red-striped", "-blue-striped",
            "-white-striped", "-ground", "-super", "-yellow", "-polar", "-cosplay",
            "-ultra", "-heart", "-snowy", "-sensu", "-eternal", "-douse", "-defense",
            "-sunshine", "-psychic", "-modern", "-natural", "-tundra", "-flying",
            "-pharaoh", "-libre", "-sunny", "-autumn", "-10", "-orange", "-standard",
            "-land", "-partner", "-dragon", "-plant", "-pirouette", "-male",
            "-hoenn-cap", "-violet", "-spring", "-fighting", "-sandstorm",
            "-original-cap", "-neutral", "-fire", "-fairy", "-attack", "-black",
            "-shock", "-shield", "-shadow", "-grass", "-continental", "-overcast",
            "-disguised", "-exclamation", "-origin", "-garden", "-blue", "-matron",
            "-red-meteor", "-small", "-rock-star", "-belle", "-alola-cap", "-green",
            "-active", "-red", "-mow", "-icy-snow", "-debutante", "-east", "-midday",
            "-jungle", "-frost", "-midnight", "-rock", "-fancy", "-busted",
            "-ordinary", "-water", "-phd", "-ice", "-spiky-eared", "-savanna",
            "-original", "-ghost", "-meadow", "-dawn", "-question", "-pom-pom",
            "-female", "-kalos-cap", "-confined", "-sinnoh-cap", "-aria", "-dark",
            "-ocean", "-wash", "-white", "-mega", "-sandy", "-complete", "-large",
            "-skylarr", "-misfit", "-doomed", "-crowned", "-raspberry", "-djspree",
            "-yuno", "-darkbritual", "-asa", "-speedy", "-curtis", "-savvy", "-brad",
            "-neuro", "-ice-rider", "-shadow-rider", "-pepe", "-zen-galar",
            "-rapid-strike", "-noice", "-hangry", "-gorging", "-gulping",
            "-aerodactyl", "-kabutops", "-hero"
        };

        if (formSuffixes.Any(suffix => name.EndsWith(suffix)))
            return true;

        if (!name.ToLower().StartsWith("unown"))
            return false;

        var unownSuffixes = new[]
        {
            "-a", "-b", "-c", "-d", "-e", "-f", "-g", "-h", "-i", "-j", "-k", "-l",
            "-m", "-n", "-o", "-p", "-q", "-r", "-s", "-t", "-u", "-v", "-w", "-x",
            "-y", "-z"
        };

        return unownSuffixes.Any(suffix => name.EndsWith(suffix));
    }
}