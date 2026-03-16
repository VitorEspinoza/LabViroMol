using Kernel;
using Kernel.Behaviors;
using LabViroMol.Modules.Inventory.Presentation;
using LabViroMol.Modules.Scheduling.Presentation;
using Mediator;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);


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
    .AddSchedulingModule(builder.Configuration);
    
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors("AngularApp");

app.UseAuthorization();

app.MapInventoryEndpoints();
app.MapSchedulingEndpoints();

app.Run();

public partial class Program;