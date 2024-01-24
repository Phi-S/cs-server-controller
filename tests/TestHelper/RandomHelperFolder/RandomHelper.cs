using System.Text;

namespace TestHelper.RandomHelperFolder;

public static class RandomHelper
{
    public static long RandomLong()
    {
        var random = new Random();
        return random.NextInt64();
    }
    
    public static int RandomInt(int maxInt = int.MaxValue)
    {
        var random = new Random();
        return random.Next(maxInt);
    }
    
    public static string RandomString(int length = 20)
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var sb = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            sb.Append(chars[random.Next(chars.Length)]);
        }

        return sb.ToString();
    }
}