using System.Text.Json.Serialization;
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
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using Mediator;
using Microsoft.AspNetCore.Authorization;
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
            .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    var permitLimit = builder.Configuration.GetValue("RateLimiting:SchedulingPolicy:PermitLimit", 5);
    var windowHours = builder.Configuration.GetValue("RateLimiting:SchedulingPolicy:WindowHours", 1);

    options.AddFixedWindowLimiter("SchedulingPolicy", opt =>
    {
        opt.PermitLimit = permitLimit;
        opt.Window = TimeSpan.FromHours(windowHours);
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
}

app.UseExceptionHandler();

// O TLS é terminado no gateway nginx (container "labviromol-gateway"); a API
// só recebe tráfego HTTP puro dentro da rede interna do compose. Sem confiar
// nos headers X-Forwarded-Proto/X-Forwarded-For que o nginx envia,
// HttpContext.Request.IsHttps fica sempre false aqui dentro - quebrando tanto
// o cookie Secure quanto o UseHttpsRedirection abaixo. KnownNetworks/KnownProxies
// ficam vazios de propósito: a porta 8080 da API nunca é publicada para fora
// do host (só o gateway nginx alcança via rede interna do Docker), então não
// há risco de um cliente externo falsificar esses headers diretamente.
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

app.MapIdentityEndpoints();
app.MapInventoryEndpoints();
app.MapResearchEndpoints();
app.MapSchedulingEndpoints();
app.MapAssetsEndpoints();
app.MapNotifyEndpoints();
app.MapAdminBffEndpoints();

app.Run();

public partial class Program;
