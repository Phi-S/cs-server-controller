using System.Diagnostics;
using Application.ServerHelperFolder;
using Xunit.Abstractions;

namespace ApplicationTests;

public class UpdateOrInstallServiceTests
{
    private readonly ITestOutputHelper _outputHelper;

    public UpdateOrInstallServiceTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public void IsServerInstalledTest()
    {
        var sw = Stopwatch.StartNew();
        var isServerInstalled = ServerHelper.IsServerInstalled(
            "\\\\wsl$\\docker-desktop-data\\data\\docker\\volumes\\CsServerController\\_data\\server");
        _outputHelper.WriteLine($"Elapsed ms: {sw.ElapsedMilliseconds}");
        _outputHelper.WriteLine($"IsServerInstalled: {isServerInstalled}");
    }
}