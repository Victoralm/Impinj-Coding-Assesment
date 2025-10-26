using Carter;
using FluentValidation;
using Polly;
using Polly.Retry;
using PreScreen_API.Features;
using PreScreen_API.Features.Interfaces;
using PreScreen_API.Models;
using Serilog;
using Serilog.Events;

namespace PreScreen_API.DI;

public static class DependencyInjection
{
    /*public static void AddingSerilog(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        var services = builder.Services;

        var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        Directory.CreateDirectory(logDirectory);
        var logFile = Path.Combine(logDirectory, "app-.log");

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: logFile,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 50 * 1024 * 1024,
                rollOnFileSizeLimit: true,
                shared: true)
            .CreateLogger();
    }*/

    public static void AddingSerilog(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var env = builder.Environment;

        // Ensure log folder exists
        var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        Directory.CreateDirectory(logDirectory);
        var logFile = Path.Combine(logDirectory, "app-.log");

        // Use Serilog integration with the Host so logs from framework + DI are captured
        builder.Host.UseSerilog((hostingContext, services, loggerConfiguration) =>
        {
            loggerConfiguration
                // read default configuration from appsettings (if any)
                .ReadFrom.Configuration(hostingContext.Configuration)
                // allow resolving services (e.g. for enrichers that use DI)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", hostingContext.HostingEnvironment.ApplicationName ?? "PreScreen_API")
                .Enrich.WithProperty("Environment", hostingContext.HostingEnvironment.EnvironmentName)
                .MinimumLevel.Is(LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .WriteTo.File(
                    path: logFile,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    fileSizeLimitBytes: 50 * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    shared: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                );

            // Add console sink in Development for convenience
            if (hostingContext.HostingEnvironment.IsDevelopment())
            {
                loggerConfiguration.WriteTo.Console();
            }
        });

        // Optional: capture unhandled exceptions early (will be logged by Serilog once UseSerilog is configured)
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        };

        // Ensure orderly flush on process exit
        AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();
    }

    public static IServiceCollection AddingBasicServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "API", Version = "v1" });
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddCarter();

        services.AddTransient<CsvParserCommandHandler>();
        services.AddTransient<ICommandHandler<CsvParserCommand, ResultDto<SalesSummaryDto>>, CsvParserCommandHandler>();

        return services;
    }

    public static IServiceCollection AddingResiliencePipeline(this IServiceCollection services)
    {
        services.AddResiliencePipeline(
            "default",
            x =>
            {
                x.AddRetry(
                        new RetryStrategyOptions
                        {
                            ShouldHandle = new Polly.PredicateBuilder().Handle<Exception>(),
                            Delay = TimeSpan.FromSeconds(2),
                            MaxRetryAttempts = 5,
                            BackoffType = DelayBackoffType.Exponential,
                            UseJitter = true,
                        }
                    )
                    .AddTimeout(TimeSpan.FromSeconds(30));
            }
        );

        return services;
    }

    public static WebApplication UseApiServices(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.MapCarter();

        return app;
    }
}
