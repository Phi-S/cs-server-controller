using System.Numerics;

namespace CustomCommandsPlugin;

public record PlayerPosition(
    int PlayerId,
    string PlayerName,
    float PositionX,
    float PositionY,
    float PositionZ,
    float AngleX,
    float AngleY,
    float AngleZ
);