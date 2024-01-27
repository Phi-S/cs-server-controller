using System.Reflection;
using System.Runtime.CompilerServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.DependencyInjection;
using PracPlugin.Services;

namespace PracPlugin;

public class PracPluginServiceCollection : IPluginServiceCollection<PracPlugin>
{
    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IBaseService, BotService>();
        serviceCollection.AddSingleton<IBaseService, GrenadeService>();
        serviceCollection.AddSingleton<IBaseService, HtmlMenuService>();
        serviceCollection.AddSingleton<IBaseService, SpawnsService>();

        serviceCollection.AddSingleton<TimerService>();
    }
}

public class PracPlugin : BasePlugin
{
    public PracPlugin(IServiceProvider serviceProvider)
    {
        serviceProvider.GetServices<IBaseService>()
            .ToList()
            .ForEach(s => s.Register(this));
    }

    public override string ModuleName => Assembly.GetExecutingAssembly().GetName().Name ??
                                         throw new NullReferenceException("AssemblyName");

    public override string ModuleVersion => "1";

    private const string ConfigName = "prac.cfg";

    public override void Load(bool hotReload)
    {
        var configSrcPath = Path.Combine(
            Server.GameDirectory,
            "csgo",
            "addons",
            "counterstrikesharp",
            "plugins",
            ModuleName,
            "Configs",
            ConfigName);

        if (File.Exists(configSrcPath) == false)
        {
            Console.WriteLine($"Failed to load {ModuleName}. \"prac.cfg\" not found");
            return;
        }

        var configDestPath = Path.Combine(
            Server.GameDirectory,
            "csgo",
            "cfg",
            ConfigName);

        if (File.Exists(configDestPath) == false)
        {
            File.Copy(configSrcPath, configDestPath, true);
        }

        Server.ExecuteCommand($"exec {ConfigName}");
        base.Load(hotReload);
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventGameStart eventGameStart, GameEventInfo info)
    {
        Console.WriteLine("OnRoundStart....");
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnTest(EventGameStart eventGameStart, GameEventInfo info)
    {
        Console.WriteLine("On game start....");
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectedFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        // Execute prac config after first player joins
        var playerCount = Utilities.GetPlayers().Count(p => p.IsBot == false);
        if (playerCount == 1)
        {
            Server.ExecuteCommand($"exec {ConfigName}");
        }

        return HookResult.Continue;
    }

    [ConsoleCommand("reload", "Executed the prac.cfg")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnPracCommand(CCSPlayerController? player, CommandInfo command)
    {
        Server.ExecuteCommand($"exec {ConfigName}");
    }
}