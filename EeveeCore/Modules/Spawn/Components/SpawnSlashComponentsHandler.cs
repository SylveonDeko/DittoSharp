using EeveeCore.Common.ModuleBases;
using Services_SpawnService = EeveeCore.Modules.Spawn.Services.SpawnService;
using SpawnService = EeveeCore.Modules.Spawn.Services.SpawnService;

namespace EeveeCore.Modules.Spawn.Components;

public class SpawnSlashComponentsHandler(DiscordShardedClient client) : EeveeCoreSlashModuleBase<Services_SpawnService>
{
}