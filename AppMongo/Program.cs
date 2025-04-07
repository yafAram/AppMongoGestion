using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using AppMongo;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de servicios
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();  // <- Agregado

// 2. Configuración MongoDB
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDB"));

builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    return new MongoClient(settings.ConnectionString);  // Simplificado
});

// 3. Registro de servicios
builder.Services.AddScoped<SecurityService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddSingleton<ExportImportController>();

var app = builder.Build();

// 4. Configuración del pipeline (solo desarrollo)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Database/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// 5. Ruteo CORREGIDO
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Database}/{action=Index}/{id?}");  // <- Cambiado

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();

public class MongoDBSettings
{
    public required string ConnectionString { get; set; }
}