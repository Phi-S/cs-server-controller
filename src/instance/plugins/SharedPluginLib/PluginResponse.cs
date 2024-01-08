using System.Text.Json;

namespace SharedPluginLib;

public record PluginResponse(bool Success, string DataJson)
{
    public bool TryGetData<T>(out T? data)
    {
        if (Success)
        {
            data = JsonSerializer.Deserialize<T>(DataJson);
            return true;
        }

        data = default;
        return false;
    }

    public static PluginResponse? GetFromJson(string json)
    {
        return JsonSerializer.Deserialize<PluginResponse>(json);
    }
    
    public static string GetSuccessJson(object data)
    {
        var dataJson = JsonSerializer.Serialize(data);
        return new PluginResponse(true, dataJson).ToJson();
    }

    public static string GetFailedJson(string message)
    {
        return new PluginResponse(false, message).ToJson();
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }
}