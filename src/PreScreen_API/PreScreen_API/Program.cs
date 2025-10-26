using PreScreen_API.DI;

var builder = WebApplication.CreateBuilder(args);

builder.AddingSerilog();

#region Dependency Injections
builder.Services
    .AddingBasicServices()
    .AddingResiliencePipeline();
#endregion

var app = builder.Build();

app.UseApiServices();

app.Run();
