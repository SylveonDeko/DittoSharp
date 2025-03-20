using Ditto.Database.Models.PostgreSQL.Pokemon;

namespace Ditto.Services;

public interface IPokemonCreationService
{
    Task<Pokemon> CreatePokemon(
        ulong userId,
        string pokemonName,
        bool shiny = false,
        bool boosted = false,
        bool radiant = false,
        string skin = null,
        string gender = null,
        int level = 1);

    Task<bool> RemovePokemon(ulong userId, int pokemonId, bool delete = false);
}