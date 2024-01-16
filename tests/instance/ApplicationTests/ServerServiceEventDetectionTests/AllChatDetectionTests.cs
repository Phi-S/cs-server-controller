using Application.EventServiceFolder;
using Application.EventServiceFolder.EventArgs;
using Application.ServerServiceFolder;
using Infrastructure.Database.Models;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using TestHelper.TestSetup;
using TestHelper.WaitUtilFolder;
using Xunit.Abstractions;

namespace ApplicationTests.ServerServiceEventDetectionTests;

public class AllChatDetectionTests
{
    private readonly ITestOutputHelper _output;

    public AllChatDetectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(
        """L 01/08/2024 - 19:36:59: "PhiS  :) < --L<5><2><[U:1:84675937]><TERRORIST>" say_team "kjkoo" """,
        "PhiS  :) < --L<5>",
        2,
        "U:1:84675937",
        Team.T,
        Chat.Team,
        "kjkoo"
    )]
    [InlineData(
        """L 01/08/2024 - 19:31:36: "PhiS :)<2><[U:1:84675937]><TERRORIST>" say "jasdkfjaskdj" """,
        "PhiS :)",
        2,
        "U:1:84675937",
        Team.T,
        Chat.All,
        "jasdkfjaskdj"
    )]
    [InlineData(
        """L 01/08/2024 - 19:32:48: "PhiS :)<2><[U:1:84675937]><CT>" say_team "gfggrr" """,
        "PhiS :)",
        2,
        "U:1:84675937",
        Team.CT,
        Chat.Team,
        "gfggrr"
    )]
    [InlineData(
        """L 01/08/2024 - 19:36:59: "PhiS > :) < --L<2><[U:1:84675937]><TERRORIST>" say_team "kjkoo" """,
        "PhiS > :) < --L",
        2,
        "U:1:84675937",
        Team.T,
        Chat.Team,
        "kjkoo"
    )]
    [InlineData(
        """L 01/08/2024 - 19:36:57: "PhiS > :) < --L<2><[U:1:84675937]><TERRORIST>" say "iklikdf" """,
        "PhiS > :) < --L",
        2,
        "U:1:84675937",
        Team.T,
        Chat.All,
        "iklikdf"
    )]
    [InlineData(
        """L 01/08/2024 - 19:36:45: "PhiS > :) < --L<2><[U:1:84675937]><CT>" say "1fgg" """,
        "PhiS > :) < --L",
        2,
        "U:1:84675937",
        Team.CT,
        Chat.All,
        "1fgg"
    )]
    [InlineData(
        """L 01/08/2024 - 19:36:48: "PhiS > :) < --L<2><[U:1:84675937]><CT>" say_team "ll" """,
        "PhiS > :) < --L",
        2,
        "U:1:84675937",
        Team.CT,
        Chat.Team,
        "ll"
    )]
    public async Task AllChatDetectionTest(string rawMessage,
        string shouldBePlayerName,
        int shouldBeUserId,
        string shouldBeSteamId3,
        Team shouldBeTeam,
        Chat shouldBeChat,
        string shouldBeMessage)
    {
        // Arrange
        var applicationServices = await ServicesSetup.GetApplicationCollection(_output);
        await using var provider = applicationServices.BuildServiceProvider();
        var eventService = provider.GetRequiredService<EventService>();
        var serverService = provider.GetRequiredService<ServerService>();

        // Act
        CustomEventArgChatMessage? arg = null;
        eventService.ChatMessage += (_, message) => { arg = message; };
        serverService.NewOutputAllChatDetection(
            null,
            new ServerOutputEventArg(
                new ServerStartDbModel
                {
                    Id = Guid.NewGuid(),
                    StartParameters = "-startParameter",
                    StartedUtc = DateTime.UtcNow,
                    CreatedUtc = DateTime.UtcNow
                },
                rawMessage));
        var waitResult =
            await WaitUtil.WaitUntil(TimeSpan.FromSeconds(1), () => arg is not null, _output.WriteLine);

        // Assert
        Assert.True(waitResult.IsError == false, waitResult.IsError ? waitResult.ErrorMessage() : "");
        Assert.True(arg is not null);
        Assert.True(arg.EventName == Events.ChatMessage);
        Assert.Equal(arg.PlayerName, shouldBePlayerName);
        Assert.Equal(arg.UserId, shouldBeUserId);
        Assert.Equal(arg.SteamId, shouldBeSteamId3);
        Assert.Equal(arg.Team, shouldBeTeam);
        Assert.Equal(arg.Chat, shouldBeChat);
        Assert.Equal(arg.Message, shouldBeMessage);
    }
}