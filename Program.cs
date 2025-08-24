using Microsoft.OpenApi.Models;
using System.IO;
using SpotAward;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Spot Award API",
        Version = "v1"
    });
    
    // Set the comments path for the Swagger JSON and UI
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    
    // Removed the SwaggerOperationFilter as it's no longer needed
});

var app = builder.Build();

// Enable Swagger always (Dev + Prod)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Spot Award API v1");
    c.RoutePrefix = string.Empty; // Swagger opens at root URL
});

app.UseAuthorization();

app.MapControllers();

app.Run();
