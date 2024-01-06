using Application;
using Domain;
using Instance.Endpoints;
using Instance.Middleware;
using Instance.SignalR;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;

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

    builder.Host.UseSerilog((_, services, configuration) =>
    {
        var options = services.GetRequiredService<IOptions<AppOptions>>();
        configuration.Enrich.WithProperty("ApplicationName", options.Value.APP_NAME)
            .Enrich.FromLogContext()
            .WriteTo.Console(expressionTemplate)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning);

        if (string.IsNullOrWhiteSpace(options.Value.SEQ_URL) == false)
        {
            configuration.WriteTo.Seq(options.Value.SEQ_URL);
        }
    });

    builder.Services.AddScoped<ApiLogMiddleware>();
    builder.Services.AddScoped<GlobalExceptionHandlerMiddleware>();

    builder.Services.AddApplication();

    builder.Services.AddSingleton<SignalRUserService>();
    builder.Services.AddSignalR();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

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

    app.UseMiddleware<ApiLogMiddleware>();
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapServerEndpoints();
    app.MapLogsEndpoints();

    app.MapHub<SignalRHub>("/hub");

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