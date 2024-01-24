﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using ErrorOr;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PracPlugin.ErrorsExtension;
using PracPlugin.Models;

namespace PracPlugin.Services;

public class HtmlMenuService : BackgroundService
{
    private readonly ILogger<HtmlMenuService> _logger;
    private readonly PracPlugin _plugin;

    public HtmlMenuService(ILogger<HtmlMenuService> logger, PracPlugin plugin)
    {
        _logger = logger;
        _plugin = plugin;
    }


    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _plugin.RegisterListener<Listeners.OnTick>(OnTick);
        _logger.LogInformation("HtmlMenuService event handler registered");

        _plugin.AddCommand("test", "test html message", CommandHandlerTest);
        _logger.LogInformation("HtmlMenuService commands registered");

        return Task.CompletedTask;
    }


    private readonly ThreadSaveDictionary<CCSPlayerController, HtmlMenuModel> _htmlMenus = new();

    #region Events

    private void OnTick()
    {
        foreach (var htmlMenusValue in _htmlMenus.Values)
        {
            htmlMenusValue.Player.PrintToCenterHtml(htmlMenusValue.Html);
        }
    }

    #endregion


    #region Commands

    private void CommandHandlerTest(CCSPlayerController? player, CommandInfo commandinfo)
    {
        ShowHtmlMessage(player, "asdf");
    }

    #endregion

    public ErrorOr<Success> ShowHtmlMessage(CCSPlayerController? player, string html)
    {
        if (player is null || player.IsValid == false)
        {
            return Errors.Fail("Player is not valid");
        }

        Server.NextFrame(() =>
        {
            var htmlMenu = new HtmlMenuModel(player, html);
            _htmlMenus.AddOrUpdate(player, htmlMenu);
        });

        return Result.Success;
    }

    public ErrorOr<Success> Hide(CCSPlayerController? player)
    {
        if (player is null || player.IsValid == false)
        {
            return Errors.Fail("Player is not valid");
        }

        if (_htmlMenus.Remove(player))
        {
            return Result.Success;
        }

        return Errors.Fail("Failed to hide html menu for player");
    }
}