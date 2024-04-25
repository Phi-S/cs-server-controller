using ErrorOr;

namespace Application.PluginsFolder;

public static class CounterStrokeSharpAdditionalAction
{
    public static async Task<ErrorOr<Success>> AddDefaultCoreConfig(string csgoFolder)
    {
        var coreJsonPath = Path.Combine(csgoFolder, "addons", "counterstrikesharp", "configs", "core.json");
        const string json =
            """
            {
                "PublicChatTrigger": [ ".", "!" ],
                "SilentChatTrigger": [ "/" ],
                "FollowCS2ServerGuidelines": true,
                "PluginHotReloadEnabled": true,
                "ServerLanguage": "en"
            }
            """;

        await File.WriteAllTextAsync(coreJsonPath, json);
        return Result.Success;
    }
}