using Microsoft.Extensions.Options;
using MongoDB.Driver;
using AppMongo.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AppMongo;

var builder = WebApplication.CreateBuilder(args);

// Configuración de servicios
builder.Services.AddControllersWithViews();
builder.Services.AddLogging(configure => configure.AddConsole());

// Configuración MongoDB con reintentos
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDB"));

builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    var clientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);

    clientSettings.ServerApi = new ServerApi(ServerApiVersion.V1);
    clientSettings.ConnectTimeout = TimeSpan.FromSeconds(30);
    clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
    clientSettings.MaxConnectionPoolSize = 100;
    clientSettings.RetryReads = true;
    clientSettings.RetryWrites = true;
    clientSettings.SocketTimeout = TimeSpan.FromSeconds(30);

    return new MongoClient(clientSettings);
});

// Registro de servicios
builder.Services.AddScoped<SecurityService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddSingleton<ExportImportService>();

var app = builder.Build();

// Configuración del pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

// Verificación de conexión a MongoDB
using (var scope = app.Services.CreateScope())
{
    try
    {
        var client = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        await client.ListDatabaseNamesAsync();
        Debug.WriteLine("Conexión a MongoDB verificada");
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error de conexión a MongoDB: {ex.Message}");
        throw;
    }
}

app.Run();

public class MongoDBSettings
{
    public required string ConnectionString { get; set; }
    public string DatabaseName { get; set; } = "admin";
    public bool EnableSSL { get; set; } = false;
}