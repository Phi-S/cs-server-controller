using Infrastructure.Database.Models;

namespace Application.ServerServiceFolder;

public class ServerOutputEventArg(ServerStart serverStart, string output) : EventArgs
{
    public ServerStart ServerStart { get; } = serverStart;

    public string Output { get; } = string.IsNullOrWhiteSpace(output)
        ? throw new NullReferenceException(nameof(output))
        : output;
}