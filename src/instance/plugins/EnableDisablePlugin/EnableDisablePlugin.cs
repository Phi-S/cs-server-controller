using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

namespace EnableDisablePlugin;

public class EnableDisablePlugin : BasePlugin
{
    private readonly ILogger<EnableDisablePlugin> _logger;

    public EnableDisablePlugin(ILogger<EnableDisablePlugin> logger)
    {
        _logger = logger;
    }

    public override string ModuleName => Assembly.GetExecutingAssembly().GetName().Name ??
                                         throw new NullReferenceException("AssemblyName");

    public override string ModuleVersion => "1";

    private string _pluginsFolder = "";
    private string _pluginsDisabledFolder = "";

    public override void Load(bool hotReload)
    {
        _pluginsFolder = Path.Combine(
            Server.GameDirectory,
            "csgo",
            "addons",
            "counterstrikesharp",
            "plugins");

        _pluginsDisabledFolder = Path.Combine(_pluginsFolder, "disabled");

        if (Directory.Exists(_pluginsFolder) == false)
        {
            _logger.LogError("Failed to load. Plugins folder \"{PluginsFolder}\" dose not exist", _pluginsFolder);
            return;
        }

        if (Directory.Exists(_pluginsDisabledFolder) == false)
        {
            Directory.CreateDirectory(_pluginsDisabledFolder);
        }

        base.Load(hotReload);
    }

    [ConsoleCommand("enable_plugin")]
    [CommandHelper(minArgs: 1, usage: "[Plugin name]", whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void EnablePlugin(CCSPlayerController? player, CommandInfo command)
    {
        try
        {
            var plugin = command.ArgByIndex(1);
            if (string.IsNullOrWhiteSpace(plugin))
            {
                command.ReplyToCommand("Plugin parameter cant be empty");
                return;
            }

            var pluginSrcPath = Path.Combine(_pluginsDisabledFolder, plugin);
            var pluginDestPath = Path.Combine(_pluginsFolder, plugin);

            if (Directory.Exists(pluginDestPath))
            {
                command.ReplyToCommand($"Plugin \"{plugin}\" already enabled");
                return;
            }

            if (Directory.Exists(pluginSrcPath) == false)
            {
                command.ReplyToCommand($"Plugin \"{plugin}\" not found");
                return;
            }

            Directory.Move(pluginSrcPath, pluginDestPath);
            Server.ExecuteCommand($"css_plugins load {plugin}");
            command.ReplyToCommand($"Plugin \"{plugin}\" enabled");
        }
        catch (Exception e)
        {
            command.ReplyToCommand($"Failed with exception: \"{e.Message}\"");
        }
    }

    [ConsoleCommand("disable_plugin")]
    [CommandHelper(minArgs: 1, usage: "[Plugin name]", whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void DisablePlugin(CCSPlayerController? player, CommandInfo command)
    {
        try
        {
            var plugin = command.ArgByIndex(1);
            if (string.IsNullOrWhiteSpace(plugin))
            {
                command.ReplyToCommand("Plugin parameter cant be empty");
                return;
            }

            var pluginSrcPath = Path.Combine(_pluginsFolder, plugin);
            var pluginDestPath = Path.Combine(_pluginsDisabledFolder, plugin);

            if (Directory.Exists(pluginDestPath) &&
                Directory.Exists(pluginSrcPath) == false)
            {
                command.ReplyToCommand($"Plugin \"{plugin}\" already disabled");
                return;
            }

            if (Directory.Exists(pluginSrcPath) == false)
            {
                command.ReplyToCommand($"Plugin \"{plugin}\" not found");
                return;
            }

            Directory.Move(pluginSrcPath, pluginDestPath);
            Server.ExecuteCommand($"css_plugins unload {plugin}");
            command.ReplyToCommand($"Plugin \"{plugin}\" disabled");
        }
        catch (Exception e)
        {
            command.ReplyToCommand($"Failed with exception: \"{e.Message}\"");
        }
    }
}