using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using nampacx.docintel.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddSingleton<ITableTransformService, TableTransformService>()
    .AddSingleton<IContentTransformService, ContentTransformService>()
    .AddSingleton<IEntryExtractionService, EntryExtractionService>();

builder.Build().Run();
