using System.Text;

namespace SharedModelsLib;

public record StartParameters(
    string ServerName = "TheAurum.net cs2 prac server",
    string? ServerPw = null,
    int MaxPlayer = 10,
    string StartMap = "de_anubis",
    int GameMode = 1,
    int GameType = 0,
    string? AdditionalStartParameters = null)
{
    public string GetString(string port)
    {
        var sb = new StringBuilder();
        const string seperatorChar = " ";
        sb.AppendJoin(seperatorChar,
            "-dedicated",
            "-console",
            $"-port {port}",
            $"+hostname {ServerName}",
            $"-maxplayers {MaxPlayer}",
            $"+game_type {GameType}",
            $"+game_mode {GameMode}",
            $"+map {StartMap}",
            string.IsNullOrWhiteSpace(ServerPw) ? "" : $"+sv_password {ServerPw}",
            $"{AdditionalStartParameters}");

        return sb.ToString();
    }

    public override string ToString()
    {
        return $"{nameof(ServerName)}: {ServerName}, {nameof(ServerPw)}: {ServerPw}, {nameof(MaxPlayer)}: {MaxPlayer}, {nameof(StartMap)}: {StartMap}, {nameof(GameMode)}: {GameMode}, {nameof(GameType)}: {GameType}, {nameof(AdditionalStartParameters)}: {AdditionalStartParameters}";
    }
}