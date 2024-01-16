using System.Net;
using System.Text.Json;
using System.Web;
using ErrorOr;
using Microsoft.Extensions.Options;
using Shared;
using Shared.ApiModels;
using Web.Helper;
using Web.Options;
using Success = ErrorOr.Success;

namespace Web.Services;

public class InstanceApiService
{
    private readonly JsonSerializerOptions _jsonOption = new(JsonSerializerDefaults.Web);

    private readonly IOptions<AppOptions> _options;
    private readonly HttpClient _httpClient;

    public InstanceApiService(IOptions<AppOptions> options, HttpClient httpClient)
    {
        _options = options;
        _httpClient = httpClient;
    }

    #region Send

    private async Task<ErrorOr<Success>> SendWithoutResponse(HttpRequestMessage requestMessage)
    {
        try
        {
            var response = await _httpClient.SendAsync(requestMessage);
            if (response.IsSuccessStatusCode)
            {
                return Result.Success;
            }

            if (response.StatusCode != HttpStatusCode.InternalServerError)
            {
                return Errors.Fail($"Request failed with status code {response.StatusCode}");
            }

            var errorResponseJson = await response.Content.ReadAsStringAsync();
            return Errors.Fail(
                $"Request failed with status code {response.StatusCode} and ErrorResponse: {errorResponseJson}");
        }
        catch (Exception e)
        {
            return Errors.Fail($"Request failed with exception \"{e.Message}\"");
        }
    }

    private async Task<ErrorOr<string>> Send(HttpRequestMessage requestMessage)
    {
        try
        {
            var response = await _httpClient.SendAsync(requestMessage);
            if (response.IsSuccessStatusCode)
            {
                var resultJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(resultJson))
                {
                    return Errors.Fail($"Request failed. Failed to read content.");
                }

                return resultJson;
            }

            if (response.StatusCode != HttpStatusCode.InternalServerError)
            {
                return Errors.Fail($"Request failed with status code {response.StatusCode}");
            }

            var errorResponseJson = await response.Content.ReadAsStringAsync();
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorResponseJson);
            return Errors.Fail(errorResponse is null
                ? $"Request failed with status code {response.StatusCode}"
                : $"{errorResponse.Message}");
        }
        catch (Exception e)
        {
            return Errors.Fail($"Request failed with exception \"{e.Message}\"");
        }
    }

    private async Task<ErrorOr<TR>> Send<TR>(HttpRequestMessage requestMessage)
    {
        var responseJson = await Send(requestMessage);
        if (responseJson.IsError)
        {
            return responseJson.FirstError;
        }

        var response = JsonSerializer.Deserialize<TR>(responseJson.Value, _jsonOption);
        if (response is null)
        {
            return Errors.Fail("Failed to deserialize response");
        }

        return response;
    }

    private HttpRequestMessage RequestMessage(HttpMethod httpMethod, string endpoint)
    {
        var uri = new Uri(_options.Value.INSTANCE_API_ENDPOINT + endpoint);
        var request = new HttpRequestMessage
        {
            Method = httpMethod,
            RequestUri = uri
        };
        return request;
    }

    private HttpRequestMessage RequestMessage<T>(HttpMethod httpMethod, string endpoint, T jsonContent)
    {
        var uri = new Uri(_options.Value.INSTANCE_API_ENDPOINT + endpoint);
        var request = new HttpRequestMessage
        {
            Content = JsonContent.Create(jsonContent),
            Method = httpMethod,
            RequestUri = uri
        };
        return request;
    }

    private HttpRequestMessage RequestMessage(HttpMethod httpMethod, string endpoint,
        List<KeyValuePair<string, string>> queryParameters)
    {
        var uri = new Uri(_options.Value.INSTANCE_API_ENDPOINT + endpoint);
        foreach (var queryParameter in queryParameters)
        {
            var keyUrlEncoded = HttpUtility.UrlEncode(queryParameter.Key);
            var valueUrlEncoded = HttpUtility.UrlEncode(queryParameter.Value);
            uri = uri.AddParameter(keyUrlEncoded, valueUrlEncoded);
        }

        var request = new HttpRequestMessage
        {
            Method = httpMethod,
            RequestUri = uri
        };
        return request;
    }

    private HttpRequestMessage GetRequestMessage(string endpoint) => RequestMessage(HttpMethod.Get, endpoint);

    private HttpRequestMessage GetRequestMessage(string endpoint, string queryParameterKey, string queryParameterValue)
    {
        return RequestMessage(HttpMethod.Get, endpoint,
            [new KeyValuePair<string, string>(queryParameterKey, queryParameterValue)]);
    }

    private HttpRequestMessage PostRequestMessage<T>(string endpoint, T jsonContent) =>
        RequestMessage(HttpMethod.Post, endpoint, jsonContent);

    private HttpRequestMessage PostRequestMessage(string endpoint) => RequestMessage(HttpMethod.Post, endpoint);

    private HttpRequestMessage PostRequestMessage(string endpoint, List<KeyValuePair<string, string>> queryParameters)
    {
        return RequestMessage(HttpMethod.Post, endpoint, queryParameters);
    }

    private HttpRequestMessage PostRequestMessage(string endpoint, string queryParameterKey, string queryParameterValue)
    {
        return PostRequestMessage(endpoint, [new KeyValuePair<string, string>(queryParameterKey, queryParameterValue)]);
    }

    #endregion


    #region Server

    public async Task<ErrorOr<ServerInfoResponse>> Info()
    {
        var requestMessage = GetRequestMessage("/server/info");
        var result = await Send<ServerInfoResponse>(requestMessage);
        return result;
    }

    public async Task<ErrorOr<List<string>>> Events()
    {
        var requestMessage = GetRequestMessage("/server/events");
        var result = await Send<List<string>>(requestMessage);
        return result;
    }

    public async Task<ErrorOr<Guid>> StartUpdatingOrInstalling()
    {
        var requestMessage = PostRequestMessage("/server/start-updating-or-installing");
        var response = await Send<Guid>(requestMessage);
        return response;
    }

    public async Task<ErrorOr<Success>> CancelUpdatingOrInstalling(Guid id)
    {
        var requestMessage = PostRequestMessage("/server/cancel-updating-or-installing", "id", id.ToString());
        var response = await SendWithoutResponse(requestMessage);
        return response;
    }

    public async Task<ErrorOr<Success>> UpdateOrInstallPlugins()
    {
        var requestMessage = PostRequestMessage("/server/update-or-install-plugins");
        var response = await SendWithoutResponse(requestMessage);
        return response;
    }

    public async Task<ErrorOr<Success>> Start()
    {
        var requestMessage = PostRequestMessage("/server/start");
        var response = await SendWithoutResponse(requestMessage);
        return response;
    }

    public async Task<ErrorOr<Success>> Stop()
    {
        var requestMessage = PostRequestMessage("/server/stop");
        var response = await SendWithoutResponse(requestMessage);
        return response;
    }

    public async Task<ErrorOr<string>> SendCommand(string command)
    {
        var requestMessage = PostRequestMessage(
            "/server/send-command",
            "command",
            command);
        var result = await Send<string>(requestMessage);
        return result;
    }

    public async Task<ErrorOr<List<string>>> Maps()
    {
        var requestMessage = GetRequestMessage("/server/maps");
        var result = await Send<List<string>>(requestMessage);
        return result;
    }

    public async Task<ErrorOr<List<ChatCommandResponse>>> ChatCommands()
    {
        var requestMessage = GetRequestMessage("/server/chat-command/all");
        var result = await Send<List<ChatCommandResponse>>(requestMessage);
        return result;
    }

    public async Task<ErrorOr<Success>> NewChatCommand(string chatMessage, string serverCommand)
    {
        var requestMessage = PostRequestMessage("/server/chat-command/new",
        [
            new KeyValuePair<string, string>("chatMessage", chatMessage),
            new KeyValuePair<string, string>("serverCommand", serverCommand)
        ]);
        var result = await SendWithoutResponse(requestMessage);
        return result;
    }

    public async Task<ErrorOr<Success>> DeleteChatCommand(string chatMessage)
    {
        var requestMessage = PostRequestMessage(
            "/server/chat-command/delete",
            "chatMessage",
            chatMessage);
        var result = await SendWithoutResponse(requestMessage);
        return result;
    }

    public async Task<ErrorOr<StartParameters>> GetStartParameters()
    {
        var requestMessage = GetRequestMessage("/server/start-parameters/get");
        var result = await Send<StartParameters>(requestMessage);
        return result;
    }

    public async Task<ErrorOr<Success>> SetStartParameters(StartParameters startParameters)
    {
        var requestMessage = PostRequestMessage("/server/start-parameters/set", startParameters);
        var result = await SendWithoutResponse(requestMessage);
        return result;
    }

    #endregion

    #region Logs

    public async Task<ErrorOr<List<SystemLogResponse>>> LogsSystem(DateTimeOffset logsSince)
    {
        var requestMessage =
            GetRequestMessage("/logs/system", "logsSince", logsSince.ToUnixTimeMilliseconds().ToString());
        var result = await Send<List<SystemLogResponse>>(requestMessage);
        return result;
    }
    
    public async Task<ErrorOr<List<ServerLogResponse>>> LogsServer(DateTimeOffset logsSince)
    {
        var requestMessage =
            GetRequestMessage("/logs/server", "logsSince", logsSince.ToUnixTimeMilliseconds().ToString());
        var result = await Send<List<ServerLogResponse>>(requestMessage);
        return result;
    }

    public async Task<ErrorOr<List<UpdateOrInstallLogResponse>>> LogsUpdateOrInstall(DateTimeOffset logsSince)
    {
        var requestMessage = GetRequestMessage("/logs/update-or-install", "logsSince",
            logsSince.ToUnixTimeMilliseconds().ToString());
        var result = await Send<List<UpdateOrInstallLogResponse>>(requestMessage);
        return result;
    }

    public async Task<ErrorOr<List<EventLogResponse>>> LogsEvents(DateTimeOffset logsSince)
    {
        var requestMessage =
            GetRequestMessage("/logs/events", "logsSince", logsSince.ToUnixTimeMilliseconds().ToString());
        var result = await Send<List<EventLogResponse>>(requestMessage);
        return result;
    }

    public async Task<ErrorOr<List<EventLogResponse>>> LogsEvents(string eventName, DateTimeOffset logsSince)
    {
        var requestMessage = GetRequestMessage($"/logs/events/{eventName}", "logsSince",
            logsSince.ToUnixTimeMilliseconds().ToString());
        var result = await Send<List<EventLogResponse>>(requestMessage);
        return result;
    }

    #endregion
}