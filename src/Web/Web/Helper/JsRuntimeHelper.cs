using Microsoft.JSInterop;

namespace Web.Helper;

public static class JsRuntimeHelper
{
    public static async Task<int> GetBrowserTimezoneOffset(this IJSRuntime jsRuntime)
    {
        return await jsRuntime.InvokeAsync<int>("getBrowserTimezoneOffset");
    }

    public static async Task CopyToClipboard(IJSRuntime jsRuntime, string text)
    {
        await jsRuntime.InvokeVoidAsync("copyClipboard", text);
    }
}