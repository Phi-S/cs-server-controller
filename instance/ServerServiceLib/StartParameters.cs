using System.Text;

namespace ServerServiceLib;

public record StartParameters(
    string ServerName,
    string? ServerPw = null,
    int MaxPlayer = 10,
    string StartMap = "de_anubis",
    int GameMode = 1,
    int GameType = 0,
    string? AdditionalStartParameters = null)
{
    public string GetString()
    {
        var sb = new StringBuilder();
        const string seperatorChar = " ";
        sb.AppendJoin(seperatorChar,
            "-dedicated",
            "-console",
            "-port 27015",
            $"+hostname {ServerName}",
            $"-maxplayers_override {MaxPlayer}",
            $"+game_type {GameType}",
            $"+game_mode {GameMode}",
            $"+map {StartMap}",
            string.IsNullOrWhiteSpace(ServerPw) ? "" : $"+sv_password {ServerPw}",
            $"{AdditionalStartParameters}");

        return sb.ToString();
    }
}