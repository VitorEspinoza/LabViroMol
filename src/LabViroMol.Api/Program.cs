using Kernel;
using Kernel.Behaviors;
using LabViroMol.Modules.Assets.Presentation;
using LabViroMol.Modules.Inventory.Presentation;
using LabViroMol.Modules.Scheduling.Presentation;
using Mediator;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader();
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
    .AddInventoryModule(builder.Configuration)
    .AddSchedulingModule(builder.Configuration)
    .AddAssetsModule(builder.Configuration);
    
builder.Services.AddAuthorization();

var imagesPath =
    builder.Configuration["Storage:ImageFolderPath"]!;

Directory.CreateDirectory(imagesPath);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        builder.Configuration["Storage:ImageFolderPath"]!),
    RequestPath = "/images"
});

app.UseCors("AngularApp");

app.UseAuthorization();

app.MapInventoryEndpoints();
app.MapSchedulingEndpoints();
app.MapAssetsEndpoints();

app.Run();

public partial class Program;