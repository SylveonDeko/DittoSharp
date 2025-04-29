namespace EeveeCore.Common.Constants;

/// <summary>
///     Provides a collection of messages used for notifying users when a Pokémon spawns.
/// </summary>
public static class SpawnMessages
{
    /// <summary>
    ///     Gets a read-only list of varied spawn notification messages used to announce
    ///     the appearance of wild Pokémon to users.
    /// </summary>
    /// <remarks>
    ///     Includes standard alerts, humorous messages, spooky messages, tech-themed messages,
    ///     simple notifications, and quirky messages to provide variety in spawn announcements.
    /// </remarks>
    public static readonly IReadOnlyList<string> Messages = new List<string>
    {
        "Attention Trainers! A wild Pokémon has just appeared nearby!",
        "Alert! There's a Pokémon lurking in the area!",
        "Pokémon detected! Who's ready for a catch?",
        "A wild Pokémon is waiting to be discovered!",
        "Get your Poké Balls ready! A Pokémon is near!",
        "Pokémon sighting confirmed! Let the hunt begin!",
        "A mysterious Pokémon has emerged in the vicinity!",
        "Who's that Pokémon? It's waiting for you!",
        "A wild Pokémon is looking for a challenge!",
        "Rustling in the bushes! A Pokémon appears!",
        "A Pokémon is nearby. Can you feel its presence?",
        "Trainers, brace yourselves! A wild Pokémon approaches!",
        "A Pokémon has been spotted! Time for an adventure!",
        "The call of the wild! A Pokémon appears!",
        "Alert: A wild Pokémon is close! Be prepared!",
        "A hidden Pokémon has made its presence known!",
        "A wild Pokémon is on the prowl. Stay alert!",
        "Get ready, Trainers! A Pokémon is nearby!",
        "A Pokémon encounter is imminent!",
        "Prepare for a wild Pokémon encounter!",
        // Fun/Humorous Messages
        "A wild Pokémon appears! It looks like it's trying to read the chat!",
        "Whoops! A wild Pokémon just tripped into view!",
        "A wild Pokémon appears, practicing its stand-up comedy routine!",
        "Look out! A wild Pokémon just cannonballed into an invisible pool!",
        "A wild Pokémon has appeared and it's... doing interpretive dance?",
        // Spooky Messages
        "A wild Pokémon emerges from the shadows, eyes glowing ominously...",
        "In the eerie silence, a wild Pokémon appears with a sinister aura...",
        "A chilling wind blows as a wild Pokémon materializes from the mist...",
        "A wild Pokémon appears, its haunting cry echoing in the darkness...",
        // Tech-themed Messages
        "Initializing encounter... A wild Pokémon has been detected!",
        "System alert: A wild Pokémon has hacked its way into the vicinity!",
        "A wild Pokémon appears, its movements precise and mechanical!",
        "Error 404: Wild Pokémon not found... Wait...nevermind",
        // Simple Messages
        "A wild Pokémon has Spawned!",
        "Hey look it's a wild creature",
        "A wild Pokémon jumps from the shadows",
        "A wild Pokémon approaches",
        "From out of the tall grass, it's a wild Pokémon",
        "A confused wild Pokémon appears out of thin air between messages",
        "A wild Pokémon materializes from the interwebs",
        // Quirky Messages
        "HOLY COW! Its one of those there poke-mans",
        "What in tarnation! Its one a dem dere' poke-EMAN thingys",
        "AAAAAAAHHHHHHHH-a Pokémon",
        "AAA- A wild pokemans",
        "I think team rocket left one of their Pokémon here.. Catch it",
        "Is that a digimon?"
    };
}