using Infrastructure.Database.Models;

namespace Application.ServerServiceFolder;

public class ServerOutputEventArg(ServerStartDbModel serverStartDbModel, string output) : EventArgs
{
    public ServerStartDbModel ServerStartDbModel { get; } = serverStartDbModel;

    public string Output { get; } = string.IsNullOrWhiteSpace(output)
        ? throw new NullReferenceException(nameof(output))
        : output;
}