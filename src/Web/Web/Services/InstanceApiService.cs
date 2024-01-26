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
            var errorResponseModel = JsonSerializer.Deserialize<ErrorResponse>(errorResponseJson);
            return Errors.Fail(errorResponseModel is null
                ? "Request failed"
                : $"{errorResponseModel.Message}. TraceId: {errorResponseModel.TraceId}");
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

    #endregion

    #region RequestMessage

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

    private HttpRequestMessage RequestMessage<T>(
        HttpMethod httpMethod,
        string endpoint,
        List<KeyValuePair<string, string>> queryParameters,
        T jsonContent)
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
            Content = JsonContent.Create(jsonContent),
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

    private HttpRequestMessage GetRequestMessage(string endpoint, List<KeyValuePair<string, string>> queryParameters)
    {
        return RequestMessage(HttpMethod.Get, endpoint, queryParameters);
    }

    private HttpRequestMessage PostRequestMessage<T>(string endpoint, T jsonContent) =>
        RequestMessage(HttpMethod.Post, endpoint, jsonContent);

    private HttpRequestMessage PostRequestMessage<T>(string endpoint,
        List<KeyValuePair<string, string>> queryParameters, T jsonContent)
    {
        return RequestMessage(HttpMethod.Post, endpoint, queryParameters, jsonContent);
    }

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

    #region ChatCommandsEndpoint

    private const string ChatCommandsEndpoint = "/chat-commands";

    public async Task<ErrorOr<List<ChatCommandResponse>>> ChatCommands()
    {
        var requestMessage = GetRequestMessage($"{ChatCommandsEndpoint}");
        var result = await Send<List<ChatCommandResponse>>(requestMessage);
        return result;
    }

    public async Task<ErrorOr<Success>> ChatCommandNew(string chatMessage, string serverCommand)
    {
        var requestMessage = PostRequestMessage($"{ChatCommandsEndpoint}/new",
        [
            new KeyValuePair<string, string>("chatMessage", chatMessage),
            new KeyValuePair<string, string>("serverCommand", serverCommand)
        ]);
        var result = await SendWithoutResponse(requestMessage);
        return result;
    }

    public async Task<ErrorOr<Success>> ChatCommandsDelete(string chatMessage)
    {
        var requestMessage = PostRequestMessage(
            $"{ChatCommandsEndpoint}/delete",
            "chatMessage",
            chatMessage);
        var result = await SendWithoutResponse(requestMessage);
        return result;
    }

    #endregion

    #region ConfigsEndpoint

    private const string ConfigsEndpoint = "/configs";

    public async Task<ErrorOr<List<string>>> Configs()
    {
        var requestMessage = GetRequestMessage($"{ConfigsEndpoint}");
        var result = await Send<List<string>>(requestMessage);
        return result;
    }

    public async Task<ErrorOr<string>> ConfigsGetContent(string configFile)
    {
        var requestMessage = GetRequestMessage(
            $"{ConfigsEndpoint}/get-content",
            "configFile",
            configFile);
        var result = await Send<string>(requestMessage);
        return result;
    }

    public async Task<ErrorOr<Success>> ConfigsSetContent(string configFile, string content)
    {
        var requestMessage = PostRequestMessage(
            $"{ConfigsEndpoint}/set-content",
            [new KeyValuePair<string, string>("configFile", configFile)],
            content);
        var result = await SendWithoutResponse(requestMessage);
        return result;
    }

    #endregion

    #region EventsEndpoint

    private const string EventsEndpoint = "/events";

    public async Task<ErrorOr<List<string>>> Events()
    {
        var requestMessage = GetRequestMessage(EventsEndpoint);
        var result = await Send<List<string>>(requestMessage);
        return result;
    }

    public async Task<ErrorOr<List<EventLogResponse>>> EventsLogs(DateTimeOffset logsSince)
    {
        var requestMessage = GetRequestMessage($"{EventsEndpoint}/logs",
            "logsSince",
            logsSince.ToUnixTimeMilliseconds().ToString());
        var result = await Send<List<EventLogResponse>>(requestMessage);
        return result;
    }

    public async Task<ErrorOr<List<EventLogResponse>>> EventsLogs(string eventName, DateTimeOffset logsSince)
    {
        var requestMessage = GetRequestMessage($"{EventsEndpoint}/logs",
            [
                new KeyValuePair<string, string>("eventName", eventName),
                new KeyValuePair<string, string>("logsSince", logsSince.ToUnixTimeMilliseconds().ToString())
            ]
        );
        var result = await Send<List<EventLogResponse>>(requestMessage);
        return result;
    }

    #endregion

    #region InfoEndpoint

    private const string InfoEndpoint = "/info";

    public async Task<ErrorOr<ServerInfoResponse>> Info()
    {
        var requestMessage = GetRequestMessage(InfoEndpoint);
        var result = await Send<ServerInfoResponse>(requestMessage);
        return result;
    }

    #endregion

    #region ServerEndpoint

    private const string ServerEndpoint = "/server";

    public async Task<ErrorOr<Success>> ServerStart()
    {
        var requestMessage = PostRequestMessage($"{ServerEndpoint}/start");
        var response = await SendWithoutResponse(requestMessage);
        return response;
    }

    public async Task<ErrorOr<Success>> ServerStop()
    {
        var requestMessage = PostRequestMessage($"{ServerEndpoint}/stop");
        var response = await SendWithoutResponse(requestMessage);
        return response;
    }

    public async Task<ErrorOr<string>> ServerSendCommand(string command)
    {
        var requestMessage = PostRequestMessage(
            $"{ServerEndpoint}/send-command",
            "command",
            command);
        var result = await Send<string>(requestMessage);
        return result;
    }

    public async Task<ErrorOr<List<ServerLogResponse>>> ServerLogs(DateTimeOffset logsSince)
    {
        var requestMessage = GetRequestMessage($"{ServerEndpoint}/logs",
            "logsSince",
            logsSince.ToUnixTimeMilliseconds().ToString());
        var result = await Send<List<ServerLogResponse>>(requestMessage);
        return result;
    }

    #endregion

    #region StartParameterEndpoint

    private const string StartParameterEndpoint = "/start-parameters";

    public async Task<ErrorOr<StartParameters>> GetStartParameters()
    {
        var requestMessage = GetRequestMessage($"{StartParameterEndpoint}");
        var result = await Send<StartParameters>(requestMessage);
        return result;
    }

    public async Task<ErrorOr<Success>> SetStartParameters(StartParameters startParameters)
    {
        var requestMessage = PostRequestMessage($"{StartParameterEndpoint}/set", startParameters);
        var result = await SendWithoutResponse(requestMessage);
        return result;
    }

    #endregion

    #region SystemEndpoint

    private const string SystemEndpoint = "/system";

    public async Task<ErrorOr<List<SystemLogResponse>>> SystemLogs(DateTimeOffset logsSince)
    {
        var requestMessage = GetRequestMessage($"{SystemEndpoint}/logs",
            "logsSince",
            logsSince.ToUnixTimeMilliseconds().ToString());
        var result = await Send<List<SystemLogResponse>>(requestMessage);
        return result;
    }

    #endregion

    #region UpdateOrInstallEndpoint

    private const string UpdateOrInstallEndpoint = "/update-or-install";

    public async Task<ErrorOr<Guid>> UpdateOrInstallStart()
    {
        var requestMessage = PostRequestMessage($"{UpdateOrInstallEndpoint}/start");
        var response = await Send<Guid>(requestMessage);
        return response;
    }

    public async Task<ErrorOr<Success>> UpdateOrInstallCancel(Guid id)
    {
        var requestMessage = PostRequestMessage($"{UpdateOrInstallEndpoint}/cancel",
            "id",
            id.ToString());
        var response = await SendWithoutResponse(requestMessage);
        return response;
    }

    public async Task<ErrorOr<Success>> UpdateOrInstallPlugins()
    {
        var requestMessage = PostRequestMessage($"{UpdateOrInstallEndpoint}/plugins");
        var response = await SendWithoutResponse(requestMessage);
        return response;
    }

    public async Task<ErrorOr<List<UpdateOrInstallLogResponse>>> UpdateOrInstallLogs(DateTimeOffset logsSince)
    {
        var requestMessage = GetRequestMessage($"{UpdateOrInstallEndpoint}/logs",
            "logsSince",
            logsSince.ToUnixTimeMilliseconds().ToString());
        var result = await Send<List<UpdateOrInstallLogResponse>>(requestMessage);
        return result;
    }

    #endregion
}