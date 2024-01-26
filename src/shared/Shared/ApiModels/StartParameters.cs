using System.Text;

namespace Shared.ApiModels;

public class StartParameters
{
    public StartParameters()
    {
    }

    public StartParameters(
        string serverHostname,
        string? serverPassword,
        int maxPlayer,
        string startMap,
        string? loginToken,
        string? additionalStartParameters)
    {
        ServerHostname = serverHostname;
        ServerPassword = serverPassword;
        MaxPlayer = maxPlayer;
        StartMap = startMap;
        LoginToken = loginToken;
        AdditionalStartParameters = additionalStartParameters;
    }

    public string ServerHostname { get; set; } = "cs2 prac server";
    public string? ServerPassword { get; set; }
    public int MaxPlayer { get; set; } = 10;
    public string StartMap { get; set; } = "de_anubis";
    public string? LoginToken { get; set; }
    public string? AdditionalStartParameters { get; set; }

    public string GetAsCommandLineArgs(string port)
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
            string.IsNullOrWhiteSpace(LoginToken) ? "" : $"+sv_setsteamaccount {LoginToken}",
            $"{AdditionalStartParameters}");

        return sb.ToString();
    }

    public override string ToString()
    {
        return
            $"{nameof(ServerHostname)}: {ServerHostname}, {nameof(ServerPassword)}: {ServerPassword}, {nameof(MaxPlayer)}: {MaxPlayer}, {nameof(StartMap)}: {StartMap}, {nameof(LoginToken)}: {LoginToken}, {nameof(AdditionalStartParameters)}: {AdditionalStartParameters}";
    }
}