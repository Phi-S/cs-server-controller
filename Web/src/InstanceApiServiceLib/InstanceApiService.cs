using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using AppOptionsLib;
using Microsoft.Extensions.Options;
using SharedModelsLib.ApiModels;
using Throw;

namespace InstanceApiServiceLib;

public class InstanceApiService(IOptions<AppOptions> options, HttpClient httpClient)
{
    private readonly JsonSerializerOptions _jsonOption = new(JsonSerializerDefaults.Web);

    #region Server

    public async Task<InfoModel> Info()
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + "/server/info";
        var json = await httpClient.GetStringAsync(url);
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var result = JsonSerializer.Deserialize<InfoModel>(json, _jsonOption);
        result.ThrowIfNull();
        return result;
    }

    public async Task<List<string>> Events()
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + "/server/events";
        var json = await httpClient.GetStringAsync(url);
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var result = JsonSerializer.Deserialize<List<string>>(json, _jsonOption);
        result.ThrowIfNull();
        return result;
    }

    public async Task<Guid> StartUpdatingOrInstalling(StartParameters? startParameters)
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + "/server/start-updating-or-installing";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (startParameters is not null)
        {
            request.Content = JsonContent.Create(startParameters);
        }

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var result = JsonSerializer.Deserialize<Guid>(json, _jsonOption);
        result.ThrowIfNull();
        return result;
    }

    public async Task<Guid> CancelUpdatingOrInstalling(Guid id)
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + $"/server/cancel-updating-or-installing?id={id.ToString()}";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var result = JsonSerializer.Deserialize<Guid>(json, _jsonOption);
        result.ThrowIfNull();
        return result;
    }

    public async Task Start(StartParameters startParameters)
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + "/server/start";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = JsonContent.Create(startParameters);
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task Stop()
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + "/server/stop";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> SendCommand(string command)
    {
        var commandUrlEncoded = HttpUtility.HtmlEncode(command);
        var url = $"{options.Value.INSTANCE_API_ENDPOINT}/server/send-command?command={commandUrlEncoded}";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(command);
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var commandResponse = await response.Content.ReadAsStringAsync();
        return commandResponse;
    }
    
    public async Task<List<string>> Maps()
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + "/server/maps";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var maps = JsonSerializer.Deserialize<List<string>>(json, _jsonOption);
        maps.ThrowIfNull();
        return maps;
    }
    
    public async Task<List<string>> Configs()
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + "/server/configs";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var configs = JsonSerializer.Deserialize<List<string>>(json, _jsonOption);
        configs.ThrowIfNull();
        return configs;
    }

    #endregion

    #region Logs

    public async Task<List<ServerLogResponse>> LogsServer(DateTimeOffset logsSince)
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + $"/logs/server?logsSince={logsSince.ToUnixTimeMilliseconds()}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var maps = JsonSerializer.Deserialize<List<ServerLogResponse>>(json, _jsonOption);
        maps.ThrowIfNull();
        return maps;
    }

    public async Task<List<UpdateOrInstallLogResponse>> LogsUpdateOrInstall(DateTimeOffset logsSince)
    {
        var url =
            $"{options.Value.INSTANCE_API_ENDPOINT}/logs/update-or-install?logsSince={logsSince.ToUnixTimeMilliseconds()}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var maps = JsonSerializer.Deserialize<List<UpdateOrInstallLogResponse>>(json, _jsonOption);
        maps.ThrowIfNull();
        return maps;
    }

    public async Task<List<EventLogResponse>> LogsEvents(DateTimeOffset logsSince)
    {
        var url = $"{options.Value.INSTANCE_API_ENDPOINT}/logs/events?logsSince={logsSince.ToUnixTimeMilliseconds()}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var maps = JsonSerializer.Deserialize<List<EventLogResponse>>(json, _jsonOption);
        maps.ThrowIfNull();
        return maps;
    }

    public async Task<List<EventLogResponse>> LogsEvents(string eventName, DateTimeOffset logsSince)
    {
        var url =
            $"{options.Value.INSTANCE_API_ENDPOINT}/logs/events/{eventName}?logsSince={logsSince.ToUnixTimeMilliseconds()}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var maps = JsonSerializer.Deserialize<List<EventLogResponse>>(json, _jsonOption);
        maps.ThrowIfNull();
        return maps;
    }

    #endregion
}