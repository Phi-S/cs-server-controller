namespace DatabaseLib;

public static class UrlUtil
{
    public static string GenerateNewUrl()
    {
        var url = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        // Replace URL unfriendly characters
        url = url
            .Replace("=", "")
            .Replace("/", "_")
            .Replace("+", "-");

        // Remove the trailing ==
        return url;
    }
}