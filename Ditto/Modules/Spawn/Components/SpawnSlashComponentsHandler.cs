using Ditto.Common.ModuleBases;
using SpawnService = Ditto.Modules.Spawn.Services.SpawnService;

namespace Ditto.Modules.Spawn.Components;

public class SpawnSlashComponentsHandler(DiscordShardedClient client) : DittoSlashModuleBase<SpawnService>
{
}