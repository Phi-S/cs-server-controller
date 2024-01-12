namespace PracPlugin.Models;

public record MapSpawnsModel(string MapName, List<PositionModel> TSpawn, List<PositionModel> CtSpawn);