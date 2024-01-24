using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;
using Shared;
using Web.Components;
using Web.Options;
using Web.Services;

var expressionTemplate = new ExpressionTemplate(
    "[{@t:yyyy-MM-dd HH:mm:ss.fff zzz} | {@l:u3}]" +
    "{#if SourceContext is not null}[{SourceContext}]{#end}" +
    "{#if RequestId is not null}[{RequestId}]{#end}" +
    " {@m}" +
    "{#if @x is not null}\n{@x}{#end}" +
    "\n",
    theme: TemplateTheme.Code);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(expressionTemplate)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddOptions<AppOptions>()
        .Bind(builder.Configuration.GetSection(AppOptions.SECTION_NAME))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Host.UseSerilog((_, services, configuration) =>
    {
        var options = services.GetRequiredService<IOptions<AppOptions>>();
        configuration.Enrich.WithProperty("ApplicationName", options.Value.APP_NAME)
            .Enrich.FromLogContext()
            .WriteTo.Console(expressionTemplate)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning);
    });

    builder.Services.AddBlazorBootstrap();
    builder.Services.AddHttpClient();
    builder.Services.AddSingleton<InstanceApiService>();
    builder.Services.AddSingleton<ServerInfoService>();

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    var app = builder.Build();

    var options = app.Services.GetRequiredService<IOptions<AppOptions>>();
    foreach (var prop in options.Value.GetType().GetProperties())
    {
        Log.Logger.Information("{Property}: {PropertyValue}",
            prop.Name, prop.GetValue(options.Value, null));
    }

    foreach (var field in options.Value.GetType().GetFields())
    {
        Log.Logger.Information("{Property}: {PropertyValue}",
            field.Name, field.GetValue(options.Value));
    }

    var serverInfoService = app.Services.GetRequiredService<ServerInfoService>();
    while (true)
    {
        var startSignalRConnection = await serverInfoService.StartSignalRConnection();
        if (startSignalRConnection.IsError)
        {
            Log.Logger.Warning("Failed to start signalr connection with error: {Error}",
                startSignalRConnection.ErrorMessage());
            Log.Logger.Information("Retrying signalr connection...");
            await Task.Delay(1000);
            continue;
        }

        break;
    }

    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseStaticFiles();
    app.UseAntiforgery();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    await app.StartAsync();
    Log.Logger.Information("Server is running under: {Addresses}", string.Join(",", app.Urls));
    Log.Logger.Information("Application started");
    await app.WaitForShutdownAsync();
}
catch (Exception e)
{
    Log.Logger.Fatal(e, "Application crashed");
}
finally
{
    await Log.CloseAndFlushAsync();
    Log.Logger.Information("Logger closed and flushed");
    Log.Logger.Information("Application exited");
}