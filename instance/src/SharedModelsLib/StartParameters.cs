using System.Text;

namespace SharedModelsLib;

public record StartParameters(
    string ServerHostname = "cs2 prac server",
    string? ServerPassword = null,
    int MaxPlayer = 10,
    string StartMap = "de_anubis",
    int GameMode = 1,
    int GameType = 0,
    string? LoginToken = null,
    string? AdditionalStartParameters = null)
{
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
    private string GetLoginToken(string? backupLoginToken)
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