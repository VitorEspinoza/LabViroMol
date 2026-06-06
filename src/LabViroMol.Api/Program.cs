using System.Text.Json.Serialization;
using LabViroMol.Modules.Assets.Presentation;
using LabViroMol.Modules.Inventory.Presentation;
using LabViroMol.Modules.Notify.Presentation;
using LabViroMol.Modules.Identity.Presentation;
using LabViroMol.Modules.Research.Presentation;
using LabViroMol.Modules.Scheduling.Presentation;
using LabViroMol.Modules.Shared.Infrastructure;
using LabViroMol.Modules.Shared.Infrastructure.Behaviors;
using LabViroMol.Modules.Shared.Infrastructure.Converters;
using Mediator;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new SmartEnumJsonConverterFactory());
    
    options.SerializerOptions.Converters.Add(new StrongIdJsonConverterFactory());
    
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddMediator(options => 
{
    options.ServiceLifetime = ServiceLifetime.Scoped; 
});
builder.Services.AddScoped(
    typeof(IPipelineBehavior<,>),
    typeof(ValidationBehavior<,>));

builder.Services
    .AddSharedModule()
    .AddIdentityModule(builder.Configuration)
    .AddInventoryModule(builder.Configuration)
    .AddSchedulingModule(builder.Configuration)
    .AddAssetsModule(builder.Configuration)
    .AddResearchModule(builder.Configuration)
    .AddNotifyModule(builder.Configuration);
    
builder.Services.AddAuthorization();

var configPath = builder.Configuration["Storage:ImageFolderPath"];
if (string.IsNullOrWhiteSpace(configPath))
{
    throw new InvalidOperationException("A configuração 'Storage:ImageFolderPath' não foi encontrada ou está vazia.");
}

var imagesPath = Path.IsPathRooted(configPath) 
    ? configPath 
    : Path.Combine(builder.Environment.ContentRootPath, configPath);

Directory.CreateDirectory(imagesPath);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagesPath),
    RequestPath = "/images"
});
app.UseCors("AngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityEndpoints();
app.MapInventoryEndpoints();
app.MapResearchEndpoints();
app.MapSchedulingEndpoints();
app.MapAssetsEndpoints();
app.MapNotifyEndpoints();

app.Run();

public partial class Program;