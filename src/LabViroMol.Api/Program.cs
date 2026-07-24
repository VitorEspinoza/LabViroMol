using System.Text.Json.Serialization;
using LabViroMol.Api.DevSeed;
using LabViroMol.Modules.AdminBff.Presentation;
using LabViroMol.Modules.Assets.Presentation;
using LabViroMol.Modules.Inventory.Presentation;
using LabViroMol.Modules.Notify.Presentation;
using LabViroMol.Modules.Identity.Presentation;
using LabViroMol.Modules.Research.Presentation;
using LabViroMol.Modules.Scheduling.Presentation;
using LabViroMol.Modules.Shared.Infrastructure;
using LabViroMol.Modules.Shared.Infrastructure.Behaviors;
using LabViroMol.Modules.Shared.Infrastructure.Converters;
using LabViroMol.Modules.Shared.Infrastructure.Observability;
using LabViroMol.Modules.Shared.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using QuestPDF.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.AddObservability();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new StrongIdJsonConverterFactory());

    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var corsAllowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition");
    });
});

builder.Services.AddRateLimiter(options =>
{
    var permitLimit = builder.Configuration.GetValue("RateLimiting:SchedulingPolicy:PermitLimit", 5);
    var windowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:SchedulingPolicy:WindowSeconds");
    var windowHours = builder.Configuration.GetValue("RateLimiting:SchedulingPolicy:WindowHours", 1);
    var window = windowSeconds.HasValue
        ? TimeSpan.FromSeconds(windowSeconds.Value)
        : TimeSpan.FromHours(windowHours);

    options.AddFixedWindowLimiter("SchedulingPolicy", opt =>
    {
        opt.PermitLimit = permitLimit;
        opt.Window = window;
    });
});

builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});
builder.Services.AddScoped(
    typeof(IPipelineBehavior<,>),
    typeof(ValidationBehavior<,>));
builder.Services.AddScoped(
    typeof(IPipelineBehavior<,>),
    typeof(LoggingBehavior<,>));

builder.Services
    .AddSharedModule()
    .AddIdentityModule(builder.Configuration)
    .AddInventoryModule(builder.Configuration)
    .AddSchedulingModule(builder.Configuration)
    .AddAssetsModule(builder.Configuration)
    .AddResearchModule(builder.Configuration)
    .AddNotifyModule(builder.Configuration)
    .AddAdminBffModule(builder.Configuration)
    .AddStorages(builder.Configuration)
    .AddTranslator(builder.Configuration)
    .AddOutboxDispatcher(builder.Configuration);

builder.Services.AddAuthorization();

builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionStringFactory: _ => builder.Configuration.ResolveLabViroMolConnectionString(),
        name: "postgres",
        tags: ["ready"]);

var configPath = builder.Configuration["Storage:RootFolder"];
if (string.IsNullOrWhiteSpace(configPath))
{
    throw new InvalidOperationException("A configuração 'Storage:RootFolder' não foi encontrada ou está vazia.");
}

var imagesPath = Path.IsPathRooted(configPath)
    ? configPath
    : Path.Combine(builder.Environment.ContentRootPath, configPath);

Directory.CreateDirectory(imagesPath);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();

    await DevSeeder.SeedAsync(app.Services, app.Configuration, app.Logger);
}

app.UseExceptionHandler();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagesPath),
    RequestPath = "/images"
});
app.UseCors("AngularApp");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false })
    .AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })
    .AllowAnonymous();

app.MapIdentityEndpoints();
app.MapInventoryEndpoints();
app.MapResearchEndpoints();
app.MapSchedulingEndpoints();
app.MapAssetsEndpoints();
app.MapNotifyEndpoints();
app.MapAdminBffEndpoints();

app.Run();

public partial class Program;
