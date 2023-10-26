using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppOptionsLib;
using Microsoft.Extensions.Options;
using SharedModelsLib;
using Throw;

namespace InstanceApiServiceLib;

public class InstanceApiService(IOptions<AppOptions> options, HttpClient httpClient)
{
    #region Server

    public async Task<InfoModel> Info()
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + "/server/info";
        var json = await httpClient.GetStringAsync(url);
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var result = JsonSerializer.Deserialize<InfoModel>(json);
        result.ThrowIfNull();
        return result;
    }

    public async Task<List<string>> Events()
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + "/server/events";
        var json = await httpClient.GetStringAsync(url);
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var result = JsonSerializer.Deserialize<List<string>>(json);
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
        var result = JsonSerializer.Deserialize<Guid>(json);
        result.ThrowIfNull();
        return result;
    }

    public async Task<Guid> CancelUpdatingOrInstalling(Guid id)
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + "/server/cancel-updating-or-installing";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = JsonContent.Create(new {id});
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var result = JsonSerializer.Deserialize<Guid>(json);
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

    public async Task<List<string>> Maps()
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + "/server/maps";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var maps = JsonSerializer.Deserialize<List<string>>(json);
        maps.ThrowIfNull();
        return maps;
    }

    public async Task<string> SendCommand(string command)
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + "/server/send-command";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = JsonContent.Create(new {command});
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var commandResponse = await response.Content.ReadAsStringAsync();
        return commandResponse;
    }

    #endregion

    #region Logs

    public async Task<List<StartLogResponse>> LogsServer(DateTime logsSince)
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + "/logs/server";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Content = JsonContent.Create(new {logsSince});
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var maps = JsonSerializer.Deserialize<List<StartLogResponse>>(json);
        maps.ThrowIfNull();
        return maps;
    }

    public async Task<List<StartLogResponse>> LogsUpdateOrInstall(DateTime logsSince)
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + "/logs/update-or-install";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Content = JsonContent.Create(new {logsSince});
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var maps = JsonSerializer.Deserialize<List<StartLogResponse>>(json);
        maps.ThrowIfNull();
        return maps;
    }

    public async Task<List<EventLogResponse>> LogsEvents(DateTime logsSince)
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + "/logs/events";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Content = JsonContent.Create(new {logsSince});
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var maps = JsonSerializer.Deserialize<List<EventLogResponse>>(json);
        maps.ThrowIfNull();
        return maps;
    }

    public async Task<List<EventLogResponse>> LogsEvents(string eventName, DateTime logsSince)
    {
        var url = options.Value.INSTANCE_API_ENDPOINT + $"/logs/events/{eventName}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Content = JsonContent.Create(new {logsSince});
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var maps = JsonSerializer.Deserialize<List<EventLogResponse>>(json);
        maps.ThrowIfNull();
        return maps;
    }

    #endregion
}