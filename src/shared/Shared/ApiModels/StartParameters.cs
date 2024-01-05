using System.Text;

namespace Shared.ApiModels;

public class StartParameters
{
    public StartParameters()
    {
    }

    public StartParameters(string serverHostname, string? serverPassword, int maxPlayer, string startMap, int gameMode,
        int gameType, string? loginToken, string? additionalStartParameters)
    {
        ServerHostname = serverHostname;
        ServerPassword = serverPassword;
        MaxPlayer = maxPlayer;
        StartMap = startMap;
        GameMode = gameMode;
        GameType = gameType;
        LoginToken = loginToken;
        AdditionalStartParameters = additionalStartParameters;
    }

    public string ServerHostname { get; set; } = "cs2 prac server";
    public string? ServerPassword { get; set; }
    public int MaxPlayer { get; set; } = 10;
    public string StartMap { get; set; } = "de_anubis";
    public int GameMode { get; set; } = 1;
    public int GameType { get; set; } = 0;
    public string? LoginToken { get; set; }
    public string? AdditionalStartParameters { get; set; }

    public string GetString(string port, string? backupLoginToken)
    {
        var sb = new StringBuilder();
        const string seperatorChar = " ";
        sb.AppendJoin(seperatorChar,
            "-dedicated",
            "-console",
            $"-port {port}",
            $"+hostname {ServerHostname}",
            string.IsNullOrWhiteSpace(ServerPassword) ? "" : $"+sv_password {ServerPassword}",
            "+log on",
            $"-maxplayers {MaxPlayer}",
            $"+map {StartMap}",
            $"+game_type {GameType}",
            $"+game_mode {GameMode}",
            GetLoginToken(backupLoginToken),
            $"{AdditionalStartParameters}");

        return sb.ToString();
    }

    /// <summary>
    /// Gets the command to set the login token.
    /// If the initial LoginToken from the StartParameter Object is not set, the backup login token will be used.
    /// If both the initial LoginToken and the backup login token are null, no token is set
    /// </summary>
    /// <param name="backupLoginToken"></param>
    /// <returns></returns>
    public string GetLoginToken(string? backupLoginToken)
    {
        if (string.IsNullOrWhiteSpace(LoginToken) == false)
        {
            return $"+sv_setsteamaccount {LoginToken}";
        }

        return string.IsNullOrWhiteSpace(backupLoginToken)
            ? ""
            : $"+sv_setsteamaccount {backupLoginToken}";
    }

    public override string ToString()
    {
        return
            $"{nameof(ServerHostname)}: {ServerHostname}, {nameof(ServerPassword)}: {ServerPassword}, {nameof(MaxPlayer)}: {MaxPlayer}, {nameof(StartMap)}: {StartMap}, {nameof(GameMode)}: {GameMode}, {nameof(GameType)}: {GameType}, {nameof(AdditionalStartParameters)}: {AdditionalStartParameters}";
    }
}