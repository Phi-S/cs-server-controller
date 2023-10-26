using Microsoft.JSInterop;

namespace web.Helper;

public static class JsRuntimeHelper
{
    public static async Task OpenNewTab(IJSRuntime jsRuntime, string uri)
    {
        await jsRuntime.InvokeAsync<object>("open", uri, "_blank");
    }

    public static async Task<int> GetBrowserTimezoneOffset(IJSRuntime jsRuntime)
    {
        return await jsRuntime.InvokeAsync<int>("getBrowserTimezoneOffset");
    }

    public static async Task<int> GetBrowserTimezoneOffsetOrUtc(IJSRuntime jsRuntime)
    {
        return await GetBrowserTimezoneOffset(jsRuntime);
    }

    public static async Task CopyToClipboard(IJSRuntime jsRuntime, string text)
    {
        await jsRuntime.InvokeVoidAsync("copyClipboard", text);
    }
}