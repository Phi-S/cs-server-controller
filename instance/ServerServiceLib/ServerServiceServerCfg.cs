namespace ServerServiceLib;

public partial class ServerService
{
    public static void CreateServerCfg(string serverFolder, string serverName, string serverPassword)
    {
        var serverCfgContent = $"""
                                // Server Defaults
                                hostname "{serverName}"
                                sv_lan 0

                                // Passwords
                                rcon_password ""
                                sv_password "{serverPassword}"
                                """;

        var serverCfgPath = Path.Combine(serverFolder, "game", "csgo", "cfg", "server.cfg");
        File.WriteAllText(serverCfgPath, serverCfgContent);
    }
}