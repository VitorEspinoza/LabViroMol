using LabViroMol.LoadTests.Data;
using LabViroMol.LoadTests.Infrastructure;
using LabViroMol.LoadTests.Scenarios;
using Microsoft.Extensions.Configuration;
using NBomber.Contracts;
using NBomber.CSharp;

var options = CommandLineOptions.Parse(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var config = LoadTestConfig.Load(configuration, options);

switch (options.Command.ToLowerInvariant())
{
    case "seed":
        await Seeder.SeedAsync(config, CancellationToken.None);
        return;
    case "reset":
        await Reset.ExecuteAsync(config.RequireConnectionString(), CancellationToken.None);
        return;
}

if (options.ResetBeforeRun)
{
    await Reset.ExecuteAsync(config.RequireConnectionString(), CancellationToken.None);
    await Seeder.SeedAsync(config, CancellationToken.None);
}

var httpClient = HttpClientFactory.Create(config);
var authClient = new AuthClient(httpClient, config);
var scenarioName = options.Scenario.ToLowerInvariant();
var requiresAuthentication = scenarioName is not ("public-read" or "public-schedule-write" or "resilience");
var requiresCatalog = scenarioName is not "public-read";
var runtime = await LoadTestRuntime.CreateAsync(
    config,
    options,
    httpClient,
    authClient,
    requiresCatalog,
    requiresAuthentication,
    CancellationToken.None);

var scenarios = ScenarioCatalog.Create(runtime);

var reportFolder = Path.Combine(AppContext.BaseDirectory, "reports", options.Campaign, options.Profile, options.Scenario);
Directory.CreateDirectory(reportFolder);

var result = NBomberRunner
    .RegisterScenarios(scenarios.ToArray())
    .WithTestSuite("LabViroMol Load Tests")
    .WithTestName($"{options.Campaign}-{options.Profile}-{options.Scenario}")
    .WithReportFolder(reportFolder)
    .Run();

ResultExporter.WriteSummary(reportFolder, runtime, result);
