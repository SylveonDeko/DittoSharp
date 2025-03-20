using Ditto.Database.DbContextStuff;
using Ditto.Database.Models.PostgreSQL.Pokemon;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ditto.Modules.Pokemon.Services;

public class ResurrectService(DbContextProvider dbProvider) : INService
{
    public async Task<List<DeadPokemon>> GetDeadPokemon(ulong userId)
    {
        await using var dbContext = await dbProvider.GetContextAsync();

        // Get the user's pokemon IDs
        var user = await dbContext.Users
            .FirstOrDefaultAsyncEF(u => u.UserId == userId);

        if (user?.Pokemon == null)
            return [];

        // Get all dead pokemon for the user's pokemon IDs
        var deadPokemon = new List<DeadPokemon>();
        foreach (var pokeId in user.Pokemon)
        {
            var deadPoke = await dbContext.DeadPokemon
                .FirstOrDefaultAsyncEF(d => d.Id == pokeId);

            if (deadPoke != null)
            {
                // Check if pokemon exists in normal pokemon table
                var existingPokemon = await dbContext.UserPokemon
                    .FirstOrDefaultAsyncEF(p => p.Id == pokeId);

                if (existingPokemon == null)
                    deadPokemon.Add(deadPoke);
            }
        }

        return deadPokemon;
    }

    public async Task ResurrectPokemon(int pokemonId)
    {
        await using var dbContext = await dbProvider.GetContextAsync();

        // Move pokemon from dead_pokes to pokes table
        await dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO pokes SELECT * FROM dead_pokes WHERE id = @p0",
            pokemonId);

        // Remove from dead_pokes
        await dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM dead_pokes WHERE id = @p0",
            pokemonId);
    }
}