using Application.EventServiceFolder;
using Application.StatusServiceFolder;
using Microsoft.Extensions.DependencyInjection;
using Shared.ApiModels;
using TestHelper.TestSetup;
using Xunit.Abstractions;

namespace ApplicationTests;

public class EventAndStatusServiceTest
{
    private readonly ITestOutputHelper _output;

    public EventAndStatusServiceTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestStartingServer()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var statusService = provider.GetRequiredService<StatusService>();

        // Act
        Assert.True(statusService.ServerStarting == false);
        eventService.OnStartingServer();

        // Assert
        Assert.True(statusService.ServerStarting);
    }

    [Fact]
    public async Task TestStartingServerDone()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var statusService = provider.GetRequiredService<StatusService>();

        // Act
        Assert.True(statusService.ServerStarted == false);

        // Assert
        eventService.OnStartingServerDone(new StartParameters());
        Assert.True(statusService.ServerStarted);
    }

    [Fact]
    public async Task TestStartingServerFailed()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var statusService = provider.GetRequiredService<StatusService>();

        // Act
        eventService.OnStartingServer();
        Assert.True(statusService.ServerStarting);
        eventService.OnStartingServerFailed();

        // Assert
        Assert.True(statusService.ServerStarting == false);
    }

    [Fact]
    public async Task TestStoppingServer()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var statusService = provider.GetRequiredService<StatusService>();

        // Act
        Assert.True(statusService.ServerStopping == false);
        eventService.OnStoppingServer();

        // Assert
        Assert.True(statusService.ServerStopping);
    }

    [Fact]
    public async Task TestServerExited()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var statusService = provider.GetRequiredService<StatusService>();

        // Act
        eventService.OnStartingServerDone(new StartParameters());
        Assert.True(statusService.ServerStarted);
        eventService.OnServerExited();

        // Assert
        Assert.True(statusService.ServerStarted == false);
    }

    [Fact]
    public async Task TestUpdateOrInstallStarted()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var statusService = provider.GetRequiredService<StatusService>();

        // Act
        Assert.True(statusService.ServerUpdatingOrInstalling == false);
        eventService.OnUpdateOrInstallStarted(Guid.NewGuid());

        // Assert
        Assert.True(statusService.ServerUpdatingOrInstalling);
    }


    [Fact]
    public async Task TestUpdateOrInstallDone()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var statusService = provider.GetRequiredService<StatusService>();

        // Act
        eventService.OnUpdateOrInstallStarted(Guid.NewGuid());
        Assert.True(statusService.ServerUpdatingOrInstalling);
        eventService.OnUpdateOrInstallDone(Guid.NewGuid());

        // Assert
        Assert.True(statusService.ServerUpdatingOrInstalling == false);
    }

    [Fact]
    public async Task TestUpdateOrInstallCancelled()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var statusService = provider.GetRequiredService<StatusService>();

        // Act
        eventService.OnUpdateOrInstallStarted(Guid.NewGuid());
        Assert.True(statusService.ServerUpdatingOrInstalling);
        eventService.OnUpdateOrInstallCancelled(Guid.NewGuid());

        // Assert
        Assert.True(statusService.ServerUpdatingOrInstalling == false);
    }

    [Fact]
    public async Task TestUpdateOrInstallFailed()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var statusService = provider.GetRequiredService<StatusService>();

        // Act
        eventService.OnUpdateOrInstallStarted(Guid.NewGuid());
        Assert.True(statusService.ServerUpdatingOrInstalling);
        eventService.OnUpdateOrInstallFailed(Guid.NewGuid());

        // Assert
        Assert.True(statusService.ServerUpdatingOrInstalling == false);
    }

    [Fact]
    public async Task TestUploadDemoStarted()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var statusService = provider.GetRequiredService<StatusService>();

        // Act
        Assert.True(statusService.DemoUploading == false);
        eventService.OnUploadDemoStarted("test");

        // Assert
        Assert.True(statusService.DemoUploading);
    }

    [Fact]
    public async Task TestUploadDemoDone()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var statusService = provider.GetRequiredService<StatusService>();

        // Act
        eventService.OnUploadDemoStarted("test");
        Assert.True(statusService.DemoUploading);
        eventService.OnUploadDemoDone("test");

        // Assert
        Assert.True(statusService.DemoUploading == false);
    }

    [Fact]
    public async Task TestUploadDemoFailed()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var statusService = provider.GetRequiredService<StatusService>();

        // Act
        eventService.OnUploadDemoStarted("test");
        Assert.True(statusService.DemoUploading);
        eventService.OnUploadDemoFailed("test");

        // Assert
        Assert.True(statusService.DemoUploading == false);
    }

    [Fact]
    public async Task TestHibernationStarted()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var statusService = provider.GetRequiredService<StatusService>();

        // Act
        Assert.True(statusService.ServerHibernating == false);
        eventService.OnHibernationStarted();

        // Assert
        Assert.True(statusService.ServerHibernating);
    }

    [Fact]
    public async Task TestHibernationEnded()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var statusService = provider.GetRequiredService<StatusService>();

        // Act
        eventService.OnHibernationStarted();
        Assert.True(statusService.ServerHibernating);
        eventService.OnHibernationEnded();

        // Assert
        Assert.True(statusService.ServerHibernating == false);
    }

    [Fact]
    public async Task TestMapChanged()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var statusService = provider.GetRequiredService<StatusService>();

        // Act
        Assert.True(string.IsNullOrWhiteSpace(statusService.CurrentMap));
        var map = "asdf";
        eventService.OnMapChanged(map);

        // Assert
        Assert.Equal(statusService.CurrentMap, map);
    }

    [Fact]
    public async Task TestPlayerConnected()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var statusService = provider.GetRequiredService<StatusService>();

        // Act
        Assert.True(statusService.CurrentPlayerCount == 0);
        eventService.OnPlayerConnected("test", "test", "test", "test");

        // Assert
        Assert.True(statusService.CurrentPlayerCount == 1);
    }

    [Fact]
    public async Task TestPlayerDisconnected()
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var statusService = provider.GetRequiredService<StatusService>();

        // Act
        eventService.OnPlayerConnected("test", "test", "test", "test");
        Assert.True(statusService.CurrentPlayerCount == 1);
        eventService.OnPlayerDisconnected("test", "test", "test", "test", "test");

        // Assert
        Assert.True(statusService.CurrentPlayerCount == 0);
    }
}